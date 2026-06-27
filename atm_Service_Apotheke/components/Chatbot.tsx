import { useState, useRef, useEffect } from 'react';
import { MessageSquare, X, Image as ImageIcon, Send, Bot, User, Loader2 } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card, CardHeader, CardTitle, CardContent, CardFooter } from '@/components/ui/card';
import { ScrollArea } from '@/components/ui/scroll-area';
import Markdown from 'react-markdown';

type Message = {
  role: 'user' | 'model';
  parts: { text: string }[];
  isImageAnalysis?: boolean;
};

export default function Chatbot() {
  const [isOpen, setIsOpen] = useState(false);
  const [messages, setMessages] = useState<Message[]>([
    {
      role: 'model',
      parts: [{ text: 'Hallo! Ich bin dein aTM-Assistent. Wie kann ich dir bei Fragen zur Assistierten Telemedizin, Ersteinschätzung oder Abrechnung helfen?' }]
    }
  ]);
  const [input, setInput] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const fileInputRef = useRef<HTMLInputElement>(null);
  const scrollRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (scrollRef.current) {
      scrollRef.current.scrollTop = scrollRef.current.scrollHeight;
    }
  }, [messages]);

  const sendMessage = async () => {
    if (!input.trim() || isLoading) return;
    
    const newMsg: Message = { role: 'user', parts: [{ text: input }] };
    setMessages(prev => [...prev, newMsg]);
    setInput('');
    setIsLoading(true);

    try {
      const response = await fetch('/api/chat', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          history: messages,
          message: input
        })
      });
      const data = await response.json();
      
      setMessages(prev => [...prev, { role: 'model', parts: [{ text: data.text }] }]);
    } catch (e) {
      console.error(e);
      setMessages(prev => [...prev, { role: 'model', parts: [{ text: 'Entschuldigung, ein Fehler ist aufgetreten.' }] }]);
    } finally {
      setIsLoading(false);
    }
  };

  const handleFileUpload = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;

    const reader = new FileReader();
    reader.onload = async (event) => {
      const base64 = event.target?.result as string;
      // Extract data:image/jpeg;base64,... 
      const parts = base64.split(',');
      const mimeType = parts[0].match(/:(.*?);/)?.[1] || "image/jpeg";
      const imageBase64 = parts[1];

      setMessages(prev => [...prev, { 
        role: 'user', 
        parts: [{ text: `[Bild hochgeladen: ${file.name}]` }],
        isImageAnalysis: true 
      }]);
      setIsLoading(true);

      try {
        const response = await fetch('/api/analyze-image', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({
            imageBase64,
            mimeType,
            prompt: "Bitte analysiere dieses Bild und fasse zusammen, was du siehst. Ist es für die Telemedizin relevant?"
          })
        });
        const data = await response.json();
        
        setMessages(prev => [...prev, { role: 'model', parts: [{ text: data.text }] }]);
      } catch (err) {
        console.error(err);
        setMessages(prev => [...prev, { role: 'model', parts: [{ text: 'Fehler bei der Bildanalyse.' }] }]);
      } finally {
        setIsLoading(false);
      }
    };
    reader.readAsDataURL(file);
    if (fileInputRef.current) fileInputRef.current.value = '';
  };

  if (!isOpen) {
    return (
      <Button
        className="fixed bottom-6 right-6 w-14 h-14 rounded-full shadow-2xl bg-[#0082C8] hover:bg-[#006A9C] text-white flex items-center justify-center p-0 transition-transform hover:scale-105 z-50"
        onClick={() => setIsOpen(true)}
      >
        <MessageSquare className="w-6 h-6" />
      </Button>
    );
  }

  return (
    <Card className="fixed bottom-6 right-6 w-[400px] h-[600px] shadow-2xl flex flex-col z-50 overflow-hidden border-slate-200 animate-in slide-in-from-bottom-10">
      <CardHeader className="bg-[#0082C8] text-white p-4 flex flex-row items-center justify-between shrink-0 rounded-t-lg">
        <div className="flex items-center gap-2">
          <Bot className="w-6 h-6" />
          <CardTitle className="text-lg font-bold">aTM KI-Assistent</CardTitle>
        </div>
        <Button variant="ghost" size="icon" className="text-white hover:bg-white/20 h-8 w-8" onClick={() => setIsOpen(false)}>
          <X className="w-5 h-5" />
        </Button>
      </CardHeader>
      
      <CardContent className="flex-1 p-0 overflow-hidden bg-slate-50">
        <div 
          ref={scrollRef}
          className="h-full overflow-y-auto p-4 space-y-4"
        >
          {messages.map((msg, i) => (
            <div key={i} className={`flex ${msg.role === 'user' ? 'justify-end' : 'justify-start'}`}>
              <div className={`flex gap-3 max-w-[85%] ${msg.role === 'user' ? 'flex-row-reverse' : 'flex-row'}`}>
                <div className={`w-8 h-8 rounded-full flex items-center justify-center shrink-0 ${msg.role === 'user' ? 'bg-slate-200' : 'bg-blue-100 text-[#0082C8]'}`}>
                  {msg.role === 'user' ? <User className="w-4 h-4 text-slate-600" /> : <Bot className="w-5 h-5" />}
                </div>
                <div className={`p-3 rounded-2xl ${msg.role === 'user' ? 'bg-[#0082C8] text-white rounded-tr-none' : 'bg-white border border-slate-200 text-slate-800 rounded-tl-none shadow-sm'}`}>
                  <div className="text-sm max-w-none break-words">
                    <Markdown>{msg.parts[0].text}</Markdown>
                  </div>
                </div>
              </div>
            </div>
          ))}
          {isLoading && (
            <div className="flex justify-start">
              <div className="flex gap-3 max-w-[85%]">
                <div className="w-8 h-8 rounded-full bg-blue-100 text-[#0082C8] flex items-center justify-center shrink-0">
                  <Bot className="w-5 h-5" />
                </div>
                <div className="p-3 rounded-2xl bg-white border border-slate-200 text-slate-800 rounded-tl-none flex items-center gap-2">
                  <Loader2 className="w-4 h-4 animate-spin text-[#0082C8]" />
                  <span className="text-sm text-slate-500">Denkt nach...</span>
                </div>
              </div>
            </div>
          )}
        </div>
      </CardContent>
      
      <CardFooter className="p-3 bg-white border-t border-slate-200 shrink-0">
        <form 
          className="flex w-full items-center gap-2"
          onSubmit={(e) => { e.preventDefault(); sendMessage(); }}
        >
          <input 
            type="file" 
            accept="image/*" 
            className="hidden" 
            ref={fileInputRef} 
            onChange={handleFileUpload} 
          />
          <Button 
            type="button" 
            variant="ghost" 
            size="icon" 
            className="shrink-0 text-slate-500 hover:text-[#0082C8]"
            onClick={() => fileInputRef.current?.click()}
            title="Bild analysieren"
          >
            <ImageIcon className="w-5 h-5" />
          </Button>
          <input
            className="flex-1 h-10 px-3 rounded-md border border-slate-300 focus:outline-none focus:ring-2 focus:ring-[#0082C8] text-sm"
            placeholder="Frag mich etwas..."
            value={input}
            onChange={(e) => setInput(e.target.value)}
          />
          <Button 
            type="submit" 
            size="icon" 
            disabled={!input.trim() || isLoading}
            className="bg-[#0082C8] hover:bg-[#006A9C] text-white shrink-0"
          >
            <Send className="w-4 h-4" />
          </Button>
        </form>
      </CardFooter>
    </Card>
  );
}
