"use client";

import { useEffect, Suspense } from "react";
import { useRouter, useSearchParams } from "next/navigation";

function RedirectToResetPassword() {
  const router = useRouter();
  const searchParams = useSearchParams();

  useEffect(() => {
    const query = searchParams.toString();
    router.replace(`/reset-password${query ? `?${query}` : ""}`);
  }, [router, searchParams]);

  return <div className="text-center text-xs text-slate-500 py-12">Đang chuyển hướng...</div>;
}

export default function AuthResetPasswordRedirectPage() {
  return (
    <Suspense fallback={<div className="text-center text-xs text-slate-500 py-12">Đang tải...</div>}>
      <RedirectToResetPassword />
    </Suspense>
  );
}
