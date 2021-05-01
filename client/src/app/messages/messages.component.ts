import { HttpClient } from '@angular/common/http';
import { Component, OnInit } from '@angular/core';
import { ToastrService } from 'ngx-toastr';
import { Message } from '../_models/message';
import { Pagination } from '../_models/pagination';
import { MessageService } from '../_services/message.service';

@Component({
  selector: 'app-messages',
  templateUrl: './messages.component.html',
  styleUrls: ['./messages.component.scss']
})
export class MessagesComponent implements OnInit {
  messages: Message[];
  pagination: Pagination;
  container = 'Unread';
  pageNumber = 1;
  pageSize = 5;
  messagesLoading = false;

  constructor(private http: HttpClient, private toastr: ToastrService, private messageService: MessageService) { }

  ngOnInit(): void {
    this.loadMessages();
  }

  loadMessages(){
    this.messagesLoading = true;
    this.messageService.getMessages(this.pageNumber, this.pageSize, this.container).subscribe(result =>{
      this.messages = result.result;
      this.pagination = result.pagination;
      this.messagesLoading = false;
    });
  }

  pageChanged(event: any){
    this.pageNumber = event.page;
    this.loadMessages();
  }

  deleteMessage(id){
    this.messageService.deleteMessage(id).subscribe(res =>{
      this.messages.splice(this.messages.findIndex(m => m.id === id), 1);
    });
  }
}
