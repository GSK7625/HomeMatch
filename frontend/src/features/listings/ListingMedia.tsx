import type { ListingImageDto } from "@/types";

export function ListingMedia({ media, className, alt = "Media tin đăng", thumbnail = false }: {
  media: Pick<ListingImageDto, "imageUrl" | "mediaType" | "thumbnailUrl">;
  className?: string;
  alt?: string;
  thumbnail?: boolean;
}) {
  if (media.mediaType === "VIDEO") {
    if (thumbnail) return media.thumbnailUrl
      ? <img src={media.thumbnailUrl} alt={alt} className={className} />
      : <span className={`flex items-center justify-center bg-slate-800 text-white text-xs ${className || ""}`}>▶ Video</span>;
    return <video src={media.imageUrl} poster={media.thumbnailUrl || undefined} controls playsInline preload="metadata" aria-label={alt} className={className} />;
  }
  return <img src={media.imageUrl} alt={alt} className={className} />;
}
