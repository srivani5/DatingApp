import { Component, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss']
})
export class AppComponent implements OnInit{
  title = 'client';
  users: any;

  constructor(private http: HttpClient){
  }

  ngOnInit(): void {
    this.getUsers();
  }

  getUsers(): void {
    this.http.get('https://localhost:5001/users').subscribe(response => {
      this.users = response;
      console.log('users : ', this.users);
    }, error => {
      console.log('error : ', error);
    });
  }

}
