using BookStore.Core.DTOs;
using BookStore.Core.Entities;
using BookStore.Core.Exceptions;
using BookStore.Core.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BookStore.Data.Services;

public class PaymentService(AppDbContext db, IPaymentGateway gateway, ILogger<PaymentService> log) : IPaymentService
{
    private const string Created = "Created";
    private const string Paid = "Paid";
    private const string Failed = "Failed";

    // ---------- 1. Start a payment for an order ----------
    public async Task<CreatePaymentResponse> CreatePaymentAsync(int userId, int orderId)
    {
        // only the owner's own, still unpaid order
        var order = await db.Orders
            .AsNoTracking()
            .Where(o => o.Id == orderId && o.UserId == userId)
            .Select(o => new { o.Id, o.Status, o.TotalAmount })
            .FirstOrDefaultAsync();

        if (order is null)
            throw new AppException("Order not found.", 404);

        if (order.Status != "Pending")
            throw new AppException($"This order cannot be paid because it is {order.Status}.", 400);

        var payment = await db.Payments.FirstOrDefaultAsync(p => p.OrderId == orderId);
        if (payment?.Status == Paid)
            throw new AppException("This order is already paid.", 409);

        // The amount ALWAYS comes from the order in our database, never from the client.
        var providerPaymentId = await gateway.CreatePaymentAsync(order.TotalAmount, order.Id);

        if (payment is null)
        {
            payment = new Payment { OrderId = order.Id, Provider = gateway.Name };
            db.Payments.Add(payment);
        }

        // a retry after a failure reuses the same row with a new provider payment id
        payment.ProviderPaymentId = providerPaymentId;
        payment.Amount = order.TotalAmount;
        payment.Status = Created;
        payment.UpdatedAt = DateTime.UtcNow;

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        {
            throw new AppException("A payment for this order was just started. Please try again.", 409);
        }

        return new CreatePaymentResponse(order.Id, gateway.Name, providerPaymentId, order.TotalAmount);
    }

    // ---------- 2. Ask for the status (the browser can check this after paying) ----------
    public async Task<PaymentStatusDto?> GetStatusAsync(int userId, int orderId)
    {
        var order = await db.Orders
            .AsNoTracking()
            .Where(o => o.Id == orderId && o.UserId == userId)
            .Select(o => new { o.Id, o.Status })
            .FirstOrDefaultAsync();

        if (order is null) return null;

        var paymentStatus = await db.Payments
            .AsNoTracking()
            .Where(p => p.OrderId == orderId)
            .Select(p => p.Status)
            .FirstOrDefaultAsync();

        return new PaymentStatusDto
        {
            OrderId = order.Id,
            OrderStatus = order.Status,
            PaymentStatus = paymentStatus ?? "NotStarted"
        };
    }

    // ---------- 3. The provider tells us the result (webhook) ----------
    public async Task<bool> HandleWebhookAsync(string body, string? signature)
    {
        var evt = gateway.ReadWebhook(body, signature);
        if (evt is null)
        {
            log.LogWarning("Payment webhook rejected: wrong signature or unreadable message");
            return false;
        }

        switch (evt.Status)
        {
            case "succeeded":
                await MarkPaidAsync(evt.ProviderPaymentId);
                break;
            case "failed":
                await MarkFailedAsync(evt.ProviderPaymentId);
                break;
            default:
                log.LogWarning("Payment webhook with unknown status {Status}", evt.Status);
                break;
        }

        return true;   // answer 200 for every valid message, so the provider stops retrying
    }

    private async Task MarkPaidAsync(string providerPaymentId)
    {
        // The stored procedure marks the payment AND the order as paid in one transaction.
        // It returns NULL when the payment was already processed or is unknown (safe to repeat).
        var rows = await db.Database
            .SqlQuery<MarkPaidRow>($"EXEC sales.sp_MarkPaymentSuccess {providerPaymentId}")
            .ToListAsync();

        var orderId = rows.SingleOrDefault()?.OrderId;
        if (orderId is null)
        {
            log.LogInformation("Payment {PaymentId} ignored: already processed or unknown", providerPaymentId);
            return;
        }

        // confirmation email goes into the queue (a background service sends it in BE-12)
        var info = await (
            from o in db.Orders
            join u in db.Users on o.UserId equals u.Id
            where o.Id == orderId
            select new { o.Id, o.TotalAmount, u.Name, u.Email }).FirstAsync();

        db.EmailQueue.Add(new EmailQueueItem
        {
            ToEmail = info.Email,
            Subject = $"Payment received for order #{info.Id}",
            Body = $"Hi {info.Name},\n\nWe received your payment of {info.TotalAmount:0.00} " +
                   $"for order #{info.Id}. We will start packing it now.\n\nThank you for shopping with us."
        });
        await db.SaveChangesAsync();

        log.LogInformation("Order {OrderId} paid", orderId);
    }

    private async Task MarkFailedAsync(string providerPaymentId)
    {
        var now = (DateTime?)DateTime.UtcNow;

        // only a payment that is still waiting can fail (a paid one never goes back)
        await db.Payments
            .Where(p => p.ProviderPaymentId == providerPaymentId && p.Status == Created)
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.Status, Failed)
                .SetProperty(p => p.UpdatedAt, now));
    }
}

// The one column the stored procedure returns (NULL when nothing was updated)
internal class MarkPaidRow
{
    public int? OrderId { get; set; }
}