using HomeMatch.Modules.Properties.Application.Interfaces;
using HomeMatch.Shared.Common;
using HomeMatch.Shared.Contracts;
using HomeMatch.Shared.Contracts.Leasing;
using HomeMatch.Shared.Contracts.Listings;
using HomeMatch.Shared.Contracts.Properties;

namespace HomeMatch.Modules.Properties.Application.Features.Rooms.DeleteRoom;

public sealed class DeleteRoomHandler(IPropertyRepository properties, IRoomRepository rooms,
    IRoomListingEditReader listingEdits, IListingDeletionWriter listingDeletion, IRoomDeletionGuard leasing,
    IRoomOperationLock roomOperations)
{
    public async Task<Result> HandleAsync(DeleteRoomCommand command, CancellationToken cancellationToken = default)
    {
        if (!await properties.IsOwnedAsync(command.LandlordId, command.PropertyId, cancellationToken))
            return Result.Failure(PropertiesErrors.PropertyNotFound);
        await using var roomLock = await roomOperations.AcquireAsync(command.RoomId, cancellationToken);
        var room = await rooms.GetForDeletionAsync(command.PropertyId, command.RoomId, cancellationToken);
        if (room is null) return Result.Failure(PropertiesErrors.RoomNotFound);
        if (room.DeletedAt.HasValue) return Result.Success();
        if (room.Status == "RENTED" || await leasing.HasReservingContractsAsync(room.Id, cancellationToken))
            return Result.Failure(PropertiesErrors.RoomHasContract);
        if (await listingEdits.IsRoomEditBlockedAsync(room.Id, cancellationToken))
            return Result.Failure(PropertiesErrors.RoomEditLocked);

        // Persist intent before calling another module. A repeated DELETE resumes this same operation.
        room.DeletionRequestedAt ??= DateTimeOffset.UtcNow;
        room.UpdatedAt = room.DeletionRequestedAt.Value;
        var saved = await rooms.SaveChangesAsync(cancellationToken);
        if (saved.IsFailure) return saved;
        var deleted = await listingDeletion.DeleteForRoomAsync(command.LandlordId, room.Id, room.DeletionRequestedAt.Value, cancellationToken);
        if (deleted.IsFailure) return deleted;
        room.DeletedAt = room.DeletionRequestedAt;
        return await rooms.SaveChangesAsync(cancellationToken);
    }
}
