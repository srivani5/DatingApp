import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { ToastrService } from 'ngx-toastr';
import { AccountService } from '../_services/account.service';


@Component({
  selector: 'app-register',
  templateUrl: './register.component.html',
  styleUrls: ['./register.component.scss']
})
export class RegisterComponent implements OnInit {
  model: any = {};
  @Output() cancelRegisterMethod = new EventEmitter();
  constructor(private accountService: AccountService, public toastr: ToastrService) { }

  ngOnInit(): void {
  }

  register(){
    console.log('model : ', this.model);
    this.accountService.register(this.model).subscribe(response => {
      this.cancelRegister();
    }, error => {
      console.log('error ', error);
      this.toastr.error(error.error);
    });
  }

  cancelRegister(){
    this.cancelRegisterMethod.emit(false);
  }
}
