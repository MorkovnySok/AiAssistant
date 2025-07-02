import { Component } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { environment } from '../environments/environment';

interface ChatMessage {
  sender: 'User' | 'Assistant' | 'System';
  message: string;
  time: string;
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

  constructor(private http: HttpClient) {}

  async sendMessage() {
    const input = this.userInput.trim();
    if (!input) return;
    this.messages.push({ sender: 'User', message: input, time: new Date().toLocaleTimeString() });
    this.userInput = '';
    this.loading = true;
    try {
      // Build conversation context
      const history = this.messages
        .map(m => `${m.sender}: ${m.message}`)
        .join('\n');
      const prompt = `${history}\nUser: ${input}\nAssistant:`;
      const res: any = await this.http.post(environment.apiUrl + '/completion', { prompt, useMemory: false }).toPromise();
      this.messages.push({ sender: 'Assistant', message: res.response, time: new Date().toLocaleTimeString() });
    } catch (err) {
      this.messages.push({ sender: 'System', message: 'Error: Could not get response from backend.', time: new Date().toLocaleTimeString() });
    } finally {
      this.loading = false;
    }
  }
}
