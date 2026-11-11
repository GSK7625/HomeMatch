using HomeMatch.Shared.Contracts.Listings;

namespace HomeMatch.Modules.Viewings.Infrastructure.Services;

/// <summary>
/// Bản cài đặt mặc định (Fallback) cho IListingContractReader để Module Viewings và Messaging
/// hoạt động độc lập trong khi Module Listings (Mục 3.2) đang được phát triển song song.
/// Khi Module Listings đăng ký IListingContractReader chính thức, DI sẽ ưu tiên bản chính thức.
/// </summary>
internal sealed class FallbackListingContractReader : IListingContractReader
{
    private const long DefaultLandlordId = 2; // Tài khoản mẫu landlord@homematch.vn

    private static readonly Dictionary<long, ListingContractInfo> SampleListings = new()
    {
        [1] = new ListingContractInfo(
            ListingId: 1,
            RoomId: 1,
            LandlordId: DefaultLandlordId,
            Title: "Phòng Studio full nội thất cao cấp ngõ 165 Cầu Giấy, gần ĐH Quốc Gia",
            Price: 4500000m,
            Address: "Số 18 Ngõ 165 Cầu Giấy, Phường Dịch Vọng, Quận Cầu Giấy, Hà Nội",
            PrimaryImageUrl: "https://images.unsplash.com/photo-1522708323590-d24dbb6b0267?w=800",
            IsPublished: true),
        [2] = new ListingContractInfo(
            ListingId: 2,
            RoomId: 2,
            LandlordId: DefaultLandlordId,
            Title: "Căn hộ dịch vụ 1PN tách bếp sang trọng tại Đặng Văn Ngữ, Đống Đa",
            Price: 6200000m,
            Address: "Ngõ 4 Đặng Văn Ngữ, Phường Trung Tự, Quận Đống Đa, Hà Nội",
            PrimaryImageUrl: "https://images.unsplash.com/photo-1493809842364-78817add7ffb?w=800",
            IsPublished: true),
        [3] = new ListingContractInfo(
            ListingId: 3,
            RoomId: 3,
            LandlordId: DefaultLandlordId,
            Title: "Phòng trọ gác lửng giá sinh viên gần ĐH Bách Khoa, Hai Bà Trưng",
            Price: 3200000m,
            Address: "Số 52 Ngõ 20 Tạ Quang Bửu, Phường Bách Khoa, Quận Hai Bà Trưng, Hà Nội",
            PrimaryImageUrl: "https://images.unsplash.com/photo-1512917774080-9991f1c4c750?w=800",
            IsPublished: true),
        [4] = new ListingContractInfo(
            ListingId: 4,
            RoomId: 4,
            LandlordId: DefaultLandlordId,
            Title: "Căn hộ mini 2 phòng ngủ ngõ Nguyễn Trãi, Thanh Xuân - View thoáng",
            Price: 7500000m,
            Address: "Số 12 Ngõ 330 Nguyễn Trãi, Phường Thanh Xuân Trung, Quận Thanh Xuân, Hà Nội",
            PrimaryImageUrl: "https://images.unsplash.com/photo-1560185007-cde436f6a4d0?w=800",
            IsPublished: true),
        [5] = new ListingContractInfo(
            ListingId: 5,
            RoomId: 5,
            LandlordId: DefaultLandlordId,
            Title: "Studio phong cách Bắc Âu tại Điện Biên Phủ, Bình Thạnh, TP.HCM",
            Price: 5500000m,
            Address: "Hẻm 150 Điện Biên Phủ, Phường 25, Quận Bình Thạnh, TP. Hồ Chí Minh",
            PrimaryImageUrl: "https://images.unsplash.com/photo-1505691938895-1758d7feb511?w=800",
            IsPublished: true),
        [6] = new ListingContractInfo(
            ListingId: 6,
            RoomId: 6,
            LandlordId: DefaultLandlordId,
            Title: "Phòng trọ cao cấp có ban công, gần Landmark 81, Bình Thạnh",
            Price: 4800000m,
            Address: "Số 48 Nguyễn Hữu Cảnh, Phường 22, Quận Bình Thạnh, TP. Hồ Chí Minh",
            PrimaryImageUrl: "https://images.unsplash.com/photo-1507089947368-19c1da9775ae?w=800",
            IsPublished: true)
    };

    public Task<ListingContractInfo?> GetListingByIdAsync(long listingId, CancellationToken cancellationToken = default)
    {
        if (listingId <= 0)
        {
            return Task.FromResult<ListingContractInfo?>(null);
        }

        if (SampleListings.TryGetValue(listingId, out var sample))
        {
            return Task.FromResult<ListingContractInfo?>(sample);
        }

        return Task.FromResult<ListingContractInfo?>(new ListingContractInfo(
            ListingId: listingId,
            RoomId: listingId,
            LandlordId: DefaultLandlordId,
            Title: $"Phòng cho thuê HomeMatch #{listingId}",
            Price: 4000000m,
            Address: "Hà Nội, Việt Nam",
            PrimaryImageUrl: null,
            IsPublished: true));
    }

    public Task<IReadOnlyDictionary<long, ListingContractInfo>> GetListingsByIdsAsync(
        IEnumerable<long> listingIds,
        CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<long, ListingContractInfo>();
        foreach (var id in listingIds.Distinct())
        {
            if (id <= 0) continue;

            result[id] = SampleListings.TryGetValue(id, out var sample)
                ? sample
                : new ListingContractInfo(
                    ListingId: id,
                    RoomId: id,
                    LandlordId: DefaultLandlordId,
                    Title: $"Phòng cho thuê HomeMatch #{id}",
                    Price: 4000000m,
                    Address: "Hà Nội, Việt Nam",
                    PrimaryImageUrl: null,
                    IsPublished: true);
        }

        return Task.FromResult<IReadOnlyDictionary<long, ListingContractInfo>>(result);
    }

    public Task<IReadOnlyList<long>> GetListingIdsByLandlordIdAsync(long landlordId, CancellationToken cancellationToken = default)
    {
        if (landlordId == DefaultLandlordId)
        {
            return Task.FromResult<IReadOnlyList<long>>(SampleListings.Keys.ToList());
        }

        return Task.FromResult<IReadOnlyList<long>>([]);
    }

    public Task<IReadOnlyList<long>> GetListingIdsByRoomIdAsync(long roomId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<long>>([roomId]);
    }
}
