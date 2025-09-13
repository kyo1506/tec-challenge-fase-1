using TecChallenge.Domain.Entities;
using TecChallenge.Shared.Models.Dtos.Responses;

namespace TecChallenge.Application.Extensions;

public static class MappingDtoExtension
{
    public static GameResponse MapToDto(this Game game)
    {
        return new GameResponse
        {
            Id = game.Id,
            Name = game.Name,
            Price = game.Price,
            IsActive = game.IsActive,
            CreatedAt = game.CreatedAt,
            UpdatedAt = game.UpdatedAt,
        };
    }

    public static UserLibraryResponse MapToDto(this UserLibrary userLibrary)
    {
        return new UserLibraryResponse
        {
            Id = userLibrary.Id,
            UserId = userLibrary.UserId,
            Items = userLibrary.Items.Select(x => x.MapToDto()),
        };
    }

    private static LibraryItemReponse MapToDto(this LibraryItem item)
    {
        return new LibraryItemReponse
        {
            Id = item.Id,
            GameId = item.GameId,
            UserLibraryId = item.UserLibraryId,
            PurchasedAt = item.PurchasedAt,
            PurchasePrice = item.PurchasePrice,
        };
    }

    public static PromotionResponse MapToDto(this Promotion promotion)
    {
        return new PromotionResponse
        {
            Id = promotion.Id,
            Name = promotion.Name,
            StartDate = promotion.StartDate,
            EndDate = promotion.EndDate,
            GamesOnSale = promotion.GamesOnSale.Select(x => x.MapToDto()),
        };
    }

    public static PromotionGameResponse MapToDto(this PromotionGame promotionGame)
    {
        return new PromotionGameResponse
        {
            Id = promotionGame.Id,
            PromotionId = promotionGame.PromotionId,
            GameId = promotionGame.GameId,
            DiscountPercentage = promotionGame.DiscountPercentage,
        };
    }
}
