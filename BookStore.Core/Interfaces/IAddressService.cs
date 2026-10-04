using BookStore.Core.DTOs;

namespace BookStore.Core.Interfaces;

public interface IAddressService
{
    Task<List<AddressDto>> ListAsync(int userId);
    Task<AddressDto> AddAsync(int userId, AddressRequest request);
    Task<AddressDto> ReplaceAsync(int userId, int addressId, AddressRequest request);
    Task DeleteAsync(int userId, int addressId);
}