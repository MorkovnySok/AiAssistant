import { Component } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { environment } from '../environments/environment';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';

interface ChatMessage {
  sender: 'User' | 'Assistant' | 'System';
  message: string;
  time: string;
  thinking?: string; // For assistant messages, optional
  showThinking?: boolean; // For UI state
}

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrl: './app.component.css',
  standalone: true,
  imports: [CommonModule, FormsModule],
})
export class AppComponent {
  title = 'chat-angular-client';
  messages: ChatMessage[] = [];
  userInput = '';
  loading = false;
  useMemory = true;

  constructor(private http: HttpClient, private sanitizer: DomSanitizer) {}

  async sendMessage() {
    console.log('sendMessage called');
    const input = this.userInput.trim();
    if (!input) return;
    this.messages.push({ sender: 'User', message: input, time: new Date().toLocaleTimeString() });
    this.userInput = '';
    this.loading = true;
    try {
      // Build conversation context (all messages, including the new one)
      const history = this.messages
        .map(m => `${m.sender}: ${m.message}`)
        .join('\n');
      // Do NOT append User: ${input} again!
      const prompt = `${history}\nAssistant:`;
      const res: any = await this.http.post(environment.apiUrl + '/completion', { prompt, useMemory: this.useMemory }).toPromise();
      // Parse <think>...</think> from response
      const response: string = res.response || '';
      const thinkMatch = response.match(/<think>([\s\S]*?)<\/think>/i);
      let answer = response;
      let thinking = '';
      if (thinkMatch) {
        answer = response.replace(thinkMatch[0], '').trim();
        thinking = thinkMatch[1].trim();
      }
      this.messages.push({
        sender: 'Assistant',
        message: answer,
        time: new Date().toLocaleTimeString(),
        thinking: thinking,
        showThinking: false
      });
    } catch (err) {
      this.messages.push({ sender: 'System', message: 'Error: Could not get response from backend.', time: new Date().toLocaleTimeString() });
    } finally {
      this.loading = false;
    }
  }

  toggleThinking(msg: ChatMessage) {
    msg.showThinking = !msg.showThinking;
  }

  formatAssistantMessage(msg: string): SafeHtml {
    if (!msg) return '';
    // Replace code blocks ```lang\n...```
    let html = msg.replace(/```([a-zA-Z0-9]*)\n([\s\S]*?)```/g, (match, lang, code) => {
      const language = lang ? `language-${lang}` : '';
      return `<pre class="msg-code"><code class="${language}">${code.replace(/</g, '&lt;').replace(/>/g, '&gt;')}</code></pre>`;
    });
    // Replace inline code `...`
    html = html.replace(/`([^`]+)`/g, '<code class="msg-inline-code">$1</code>');
    // Replace **bold** with <b>bold</b>
    html = html.replace(/\*\*(.+?)\*\*/g, '<b>$1</b>');
    // Replace newlines with <br> (but not inside <pre> blocks)
    html = html.replace(/(?!<pre[\s\S]*?>[\s\S]*?)(\n)/g, '<br>');
    return this.sanitizer.bypassSecurityTrustHtml(html);
  }
}
