import { Notice } from "./Notice";

// DESIGN §5 Feedback: a skeleton while loading, no spinner over the content.
export function ViewerSkeleton() {
  return <div className="mx-auto h-96 w-full max-w-[680px] animate-pulse rounded-sheet bg-rule/40" aria-hidden="true" />;
}

export function ViewerFailed() {
  return <Notice tone="error">Không hiển thị được file này trong trang. Bạn vẫn có thể tải file về để xem.</Notice>;
}
