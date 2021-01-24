import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { Observable } from 'rxjs';
import { User } from '../_models/user';
import { AccountService } from '../_services/account.service';

@Component({
  selector: 'app-nav',
  templateUrl: './nav.component.html',
  styleUrls: ['./nav.component.scss']
})
export class NavComponent implements OnInit {

  constructor(public accountService: AccountService, public router: Router, public toastr: ToastrService) { }
  model: any = {};
  loggedIn = false;
  user: User;
  ngOnInit(): void {
  }

  login(): void {
    console.log('model : ', this.model);
    this.accountService.login(this.model).subscribe(response => {
      console.log('response : ', response);
      this.model.username = '';
      this.model.password = '';
      this.router.navigateByUrl('/messages');
    }, error => {
      console.log('error : ', error);
      this.toastr.error(error.error);
    });
  }

  logOut(): void {
    this.accountService.logout();
    this.router.navigateByUrl('/');
  }

}
