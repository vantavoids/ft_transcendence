import { ChatWorkspace } from '../../src/components/chat-workspace';
import { SignalingHubProvider } from '../../src/shared/realtime/signaling-hub';
import { CallProvider } from '../../src/shared/call/call-context';

export default function ChatPage() {
  return (
    <SignalingHubProvider>
      <CallProvider>
        <ChatWorkspace />
      </CallProvider>
    </SignalingHubProvider>
  );
}
