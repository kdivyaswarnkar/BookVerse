using BookStore.Core.DTOs;
using BookStore.Core.Entities;
using BookStore.Core.Exceptions;
using BookStore.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Data.Services;

public class AddressService(AppDbContext db) : IAddressService
{
    private const int MaxAddressesPerUser = 10;

    public async Task<List<AddressDto>> ListAsync(int userId)
    {
        var addresses = await db.Addresses
            .AsNoTracking()
            .Where(a => a.UserId == userId && !a.IsDeleted)
            .OrderByDescending(a => a.CreatedAt).ThenByDescending(a => a.Id)
            .ToListAsync();

        return addresses.Select(ToDto).ToList();
    }

    public async Task<AddressDto> AddAsync(int userId, AddressRequest request)
    {
        var count = await db.Addresses.CountAsync(a => a.UserId == userId && !a.IsDeleted);
        if (count >= MaxAddressesPerUser)
            throw new AppException($"You can save at most {MaxAddressesPerUser} addresses.", 400);

        var address = Create(userId, request);
        db.Addresses.Add(address);
        await db.SaveChangesAsync();
        return ToDto(address);
    }

    // "Editing" = retire the old row and create a new one. Old orders keep pointing to the
    // address they were really sent to (the same idea as storing the price on the order item).
    public async Task<AddressDto> ReplaceAsync(int userId, int addressId, AddressRequest request)
    {
        var old = await db.Addresses
            .FirstOrDefaultAsync(a => a.Id == addressId && a.UserId == userId && !a.IsDeleted);

        if (old is null)
            throw new AppException("Address not found.", 404);

        old.IsDeleted = true;
        old.UpdatedAt = DateTime.UtcNow;

        var address = Create(userId, request);
        db.Addresses.Add(address);

        await db.SaveChangesAsync();   // both changes are saved in one transaction
        return ToDto(address);
    }

    public async Task DeleteAsync(int userId, int addressId)
    {
        var now = (DateTime?)DateTime.UtcNow;

        var rows = await db.Addresses
            .Where(a => a.Id == addressId && a.UserId == userId && !a.IsDeleted)
            .ExecuteUpdateAsync(s => s
                .SetProperty(a => a.IsDeleted, true)
                .SetProperty(a => a.UpdatedAt, now));

        if (rows == 0)
            throw new AppException("Address not found.", 404);
    }

    private static Address Create(int userId, AddressRequest r) => new()
    {
        UserId = userId,
        FullName = r.FullName.Trim(),
        Phone = r.Phone.Trim(),
        Line1 = r.Line1.Trim(),
        City = r.City.Trim(),
        State = r.State.Trim(),
        PostalCode = r.PostalCode.Trim()
    };

    private static AddressDto ToDto(Address a) => new()
    {
        Id = a.Id,
        FullName = a.FullName,
        Phone = a.Phone,
        Line1 = a.Line1,
        City = a.City,
        State = a.State,
        PostalCode = a.PostalCode
    };
}