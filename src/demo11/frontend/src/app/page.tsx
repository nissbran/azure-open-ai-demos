"use client";

import {
  useCoAgent,
  useFrontendTool
} from "@copilotkit/react-core";
import { CopilotKitCSSProperties, CopilotSidebar, CopilotChat } from "@copilotkit/react-ui";
import { useState } from "react";
import { AgentState } from "@/lib/types";

export default function CopilotKitPage() {
  const [themeColor, setThemeColor] = useState("#6366f1");

  // 🪁 Frontend Actions: https://docs.copilotkit.ai/microsoft-agent-framework/frontend-actions
  useFrontendTool({
    name: "setThemeColor",
    description: "Set the theme color of the application",
    parameters: [
      {
        name: "themeColor",
        type: "string",
        description: "The theme color to set. Make sure to pick nice colors.",
        required: true,
      },
    ],
    handler: async ({ themeColor }) => {
      setThemeColor(themeColor);
    },
  });

  return (
    <main
      style={
        { "--copilot-kit-primary-color": themeColor } as CopilotKitCSSProperties
      }
    >
      <CopilotChat
              //clickOutsideToClose={false}
              imageUploadsEnabled={true}
        labels={{
          title: "Assistant",
          initial: "👋 Hi, there! You're chatting with an agent.",
        }}
        suggestions={[
          {
            title: "Poem writing",
            message: "Write a poem about the sea.",
          },
        ]}
      >
       {/*<YourMainContent themeColor={themeColor} />*/}
      </CopilotChat>
    </main>
  );
}

function YourMainContent({ themeColor }: { themeColor: string }) {
  // 🪁 Shared State: https://docs.copilotkit.ai/pydantic-ai/shared-state
  const { state, setState } = useCoAgent<AgentState>({
    name: "chat_agent",
    initialState: {
      proverbs: [
        "CopilotKit may be new, but its the best thing since sliced bread.",
      ],
    },
  });

  return (
    <div
      style={{ backgroundColor: themeColor }}
      className="h-screen flex justify-center items-center flex-col transition-colors duration-300"
    >
      {/*<ProverbsCard state={state} setState={setState} />*/}
    </div>
  );
}
