"use client";

import { useEffect, Suspense } from "react";
import { useRouter, useSearchParams } from "next/navigation";

function RedirectToVerifyEmail() {
  const router = useRouter();
  const searchParams = useSearchParams();

  useEffect(() => {
    const query = searchParams.toString();
    router.replace(`/verify-email${query ? `?${query}` : ""}`);
  }, [router, searchParams]);

  return <div className="text-center text-xs text-slate-500 py-12">Đang chuyển hướng...</div>;
}

export default function AuthVerifyEmailRedirectPage() {
  return (
    <Suspense fallback={<div className="text-center text-xs text-slate-500 py-12">Đang tải...</div>}>
      <RedirectToVerifyEmail />
    </Suspense>
  );
}
