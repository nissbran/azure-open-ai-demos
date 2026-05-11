"use client";

import React, { useState } from "react";
import { CopilotChat, useRenderTool } from "@copilotkit/react-core/v2";
import "@copilotkit/react-core/v2/styles.css";
import { z } from "zod";

export default function CopilotKitPage() {
  useRenderTool({
    name: "think",
    parameters: z.object({ thought: z.string() }),
    render: ({ args, status }: any) => (
      <DefaultReasoningMessage thought={args?.thought ?? ""} status={status} />
    ),
  });

  return (
    <CopilotChat
      agentId="chat_agent"
      className="h-full rounded-2xl"
    />
  );
}

// Mirrors CopilotKit's built-in CopilotChatReasoningMessage UX: a
// collapsible "Thinking…" / "Thought for a moment" card.
function DefaultReasoningMessage({
  thought,
  status,
}: {
  thought: string;
  status?: string;
}) {
  const isStreaming = status !== "complete";
  const [open, setOpen] = useState(isStreaming);
  const hasContent = thought.length > 0;

  return (
    <div
      data-testid="reasoning-default"
      style={{
        margin: "8px 0",
        borderRadius: "12px",
        border: "1px solid var(--copilot-kit-separator-color, #e5e7eb)",
        background: "var(--copilot-kit-secondary-color, #f9fafb)",
        padding: "8px 12px",
        fontSize: "13px",
      }}
    >
      <button
        type="button"
        onClick={() => setOpen((v) => !v)}
        style={{
          all: "unset",
          cursor: "pointer",
          display: "flex",
          alignItems: "center",
          gap: "8px",
          fontWeight: 500,
          color: "var(--copilot-kit-muted-color, #4b5563)",
        }}
      >
        <span aria-hidden>{open ? "▾" : "▸"}</span>
        <span>{isStreaming ? "Thinking…" : "Thought for a moment"}</span>
      </button>
      {open && hasContent && (
        <div
          style={{
            marginTop: "6px",
            whiteSpace: "pre-wrap",
            color: "var(--copilot-kit-muted-color, #6b7280)",
          }}
        >
          {thought}
        </div>
      )}
    </div>
  );
}