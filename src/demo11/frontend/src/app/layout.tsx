import type { Metadata } from "next";

import { CopilotKitProvider } from "@copilotkit/react-core/v2";
import "./globals.css";
import CopilotKitPage from "@/app/page";

export const metadata: Metadata = {
  title: "CopilotKit Next.js Demo",
  description: "A demo of CopilotKit integrated into a Next.js application.",
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="en">
      <body className={"antialiased"}>
          <CopilotKitProvider runtimeUrl="/api/copilotkit">
              <CopilotKitPage />
          </CopilotKitProvider>
      </body>
    </html>
  );
}
