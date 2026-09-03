import { useEffect, useState, useRef, useCallback } from "react";
import { apiFetch } from "../api/client";
import { connectSignalR } from "../lib/signalr";

interface ChatMessage {
  id: string;
  conversationId: string;
  senderUserId: string;
  senderName: string;
  content: string;
  createdAt: string;
  isRead: boolean;
}

interface Conversation {
  id: string;
  requestId: string;
  requestTitle: string;
  customerUserId: string;
  customerName: string;
  shopId: string;
  shopName: string;
}

interface ChatDrawerProps {
  isOpen: boolean;
  onClose: () => void;
  requestId: string;
  shopId: string;
  title: string;
  otherPartyName: string;
  currentUserId: string;
}

export default function ChatDrawer({
  isOpen,
  onClose,
  requestId,
  shopId,
  title,
  otherPartyName,
  currentUserId,
}: ChatDrawerProps) {
  const [conversation, setConversation] = useState<Conversation | null>(null);
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [inputText, setInputText] = useState("");
  const [loading, setLoading] = useState(false);
  const [sending, setSending] = useState(false);
  const [typingUser, setTypingUser] = useState<string | null>(null);

  const messagesEndRef = useRef<HTMLDivElement>(null);
  const typingTimeoutRef = useRef<number | null>(null);

  const scrollToBottom = () => {
    messagesEndRef.current?.scrollIntoView({ behavior: "smooth" });
  };

  // 1. Load Conversation
  const loadConversation = useCallback(async () => {
    if (!requestId || !shopId) return;
    setLoading(true);
    try {
      const conv = await apiFetch<Conversation>(`/api/chat/request/${requestId}/shop/${shopId}`);
      setConversation(conv);

      const msgs = await apiFetch<ChatMessage[]>(`/api/chat/conversations/${conv.id}/messages`);
      setMessages(msgs);

      // Mark read
      await apiFetch(`/api/chat/conversations/${conv.id}/read`, { method: "POST" });
    } catch (err) {
      console.error("Failed to load chat conversation", err);
    } finally {
      setLoading(false);
      setTimeout(scrollToBottom, 100);
    }
  }, [requestId, shopId]);

  useEffect(() => {
    if (isOpen) {
      loadConversation();
    }
  }, [isOpen, loadConversation]);

  // 2. Real-time SignalR Listeners
  useEffect(() => {
    if (!isOpen || !conversation) return;

    let mounted = true;
    (async () => {
      try {
        const hub = await connectSignalR();
        await hub.invoke("JoinConversation", conversation.id);

        const onNewMessage = (payload: unknown) => {
          if (!mounted) return;
          const msg = payload as ChatMessage;
          if (msg.conversationId === conversation.id) {
            setMessages((prev) => {
              if (prev.some((m) => m.id === msg.id)) return prev;
              return [...prev, msg];
            });
            setTimeout(scrollToBottom, 100);
          }
        };

        const onUserTyping = (payload: unknown) => {
          if (!mounted) return;
          const data = payload as { conversationId: string; userId: string; userName: string };
          if (data.conversationId === conversation.id && data.userId !== currentUserId) {
            setTypingUser(data.userName);
            if (typingTimeoutRef.current) clearTimeout(typingTimeoutRef.current);
            typingTimeoutRef.current = window.setTimeout(() => setTypingUser(null), 2500);
          }
        };

        hub.on("NewChatMessage", onNewMessage);
        hub.on("UserTyping", onUserTyping);

        return () => {
          hub.off("NewChatMessage", onNewMessage);
          hub.off("UserTyping", onUserTyping);
          hub.invoke("LeaveConversation", conversation.id).catch(() => {});
        };
      } catch (err) {
        console.error("SignalR chat connection error", err);
      }
    })();

    return () => {
      mounted = false;
    };
  }, [isOpen, conversation, currentUserId]);

  const handleSend = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!inputText.trim() || !conversation || sending) return;

    const text = inputText.trim();
    setInputText("");
    setSending(true);

    try {
      const newMsg = await apiFetch<ChatMessage>(`/api/chat/conversations/${conversation.id}/messages`, {
        method: "POST",
        body: JSON.stringify({ content: text }),
      });
      setMessages((prev) => [...prev, newMsg]);
      setTimeout(scrollToBottom, 50);
    } catch (err) {
      console.error("Failed to send message", err);
      setInputText(text); // Restore on error
    } finally {
      setSending(false);
    }
  };

  const handleTyping = () => {
    if (!conversation) return;
    connectSignalR().then((hub) => {
      hub.invoke("SendTyping", conversation.id).catch(() => {});
    });
  };

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 z-50 flex justify-end bg-black/60 backdrop-blur-xs">
      <div className="flex h-full w-full max-w-md flex-col bg-slate-900 border-l border-slate-800 text-white shadow-2xl animate-in slide-in-from-right duration-200">
        {/* Header */}
        <div className="flex items-center justify-between border-b border-slate-800 p-4 bg-slate-950/80">
          <div className="flex items-center gap-3">
            <div className="h-10 w-10 rounded-full bg-indigo-600/30 text-indigo-400 flex items-center justify-center font-bold text-base border border-indigo-500/30">
              💬
            </div>
            <div>
              <h3 className="text-sm font-extrabold text-white truncate max-w-[200px]">{otherPartyName}</h3>
              <p className="text-[11px] text-slate-400 truncate max-w-[200px]">{title}</p>
            </div>
          </div>
          <button
            onClick={onClose}
            className="rounded-full bg-slate-800 p-2 text-slate-400 hover:text-white hover:bg-slate-700 transition"
          >
            ✕
          </button>
        </div>

        {/* Message Stream */}
        <div className="flex-1 overflow-y-auto p-4 space-y-3 bg-slate-950">
          {loading ? (
            <div className="flex justify-center py-12">
              <div className="h-6 w-6 animate-spin rounded-full border-2 border-indigo-500 border-t-transparent" />
            </div>
          ) : messages.length === 0 ? (
            <div className="flex flex-col items-center justify-center py-16 text-center text-slate-500">
              <span className="text-3xl mb-2">💬</span>
              <p className="text-xs font-semibold">Start the conversation</p>
              <p className="text-[11px] mt-1 text-slate-600 max-w-xs">
                Clarify items, availability, payment options, or pickup instructions directly.
              </p>
            </div>
          ) : (
            messages.map((m) => {
              const isMe = m.senderUserId === currentUserId;
              return (
                <div key={m.id} className={`flex flex-col ${isMe ? "items-end" : "items-start"}`}>
                  <span className="text-[10px] text-slate-500 mb-1 px-1">
                    {isMe ? "You" : m.senderName} · {new Date(m.createdAt).toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" })}
                  </span>
                  <div
                    className={`max-w-[80%] rounded-2xl px-4 py-2.5 text-xs font-medium leading-relaxed ${
                      isMe
                        ? "bg-indigo-600 text-white rounded-br-xs"
                        : "bg-slate-800 text-slate-200 rounded-bl-xs border border-slate-700"
                    }`}
                  >
                    {m.content}
                  </div>
                </div>
              );
            })
          )}
          {typingUser && (
            <div className="text-[11px] text-indigo-400 italic animate-pulse">
              {typingUser} is typing…
            </div>
          )}
          <div ref={messagesEndRef} />
        </div>

        {/* Input Bar */}
        <form onSubmit={handleSend} className="border-t border-slate-800 bg-slate-900 p-3">
          <div className="flex items-center gap-2">
            <input
              type="text"
              value={inputText}
              onChange={(e) => {
                setInputText(e.target.value);
                handleTyping();
              }}
              placeholder="Type a message…"
              className="flex-1 rounded-xl bg-slate-950 border border-slate-700 px-4 py-2.5 text-xs text-white placeholder-slate-500 focus:outline-none focus:border-indigo-500"
            />
            <button
              type="submit"
              disabled={!inputText.trim() || sending}
              className="rounded-xl bg-indigo-600 px-4 py-2.5 text-xs font-bold text-white hover:bg-indigo-500 transition disabled:opacity-40"
            >
              Send
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
