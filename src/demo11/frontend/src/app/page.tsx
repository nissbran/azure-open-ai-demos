import { CopilotChat } from "@copilotkit/react-core/v2";
import "@copilotkit/react-core/v2/styles.css";

export default function CopilotKitPage() {

  return (
    <CopilotChat
      agentId="chat_agent"
      className="h-full rounded-2xl"
    />
  );
}
