import { Component, HostListener, OnInit, ViewChild } from '@angular/core';
import { NgForm } from '@angular/forms';
import { ToastrService } from 'ngx-toastr';
import { take } from 'rxjs/operators';
import { Member } from 'src/app/_models/member';
import { User } from 'src/app/_models/user';
import { AccountService } from 'src/app/_services/account.service';
import { MemberService } from 'src/app/_services/member.service';

@Component({
  selector: 'app-member-edit',
  templateUrl: './member-edit.component.html',
  styleUrls: ['./member-edit.component.scss']
})
export class MemberEditComponent implements OnInit {

  @ViewChild('editForm') editForm: NgForm;
  user: User;
  member: Member;
  @HostListener('window:beforeunload', ['$event']) unloadNotification($event: any) {
    if (this.editForm.dirty) {
      $event.returnValue = true;
    }
  }

  constructor(private accountService: AccountService, private memberService: MemberService,
              protected toastr: ToastrService) {
    this.accountService.currentUser.pipe(take(1)).subscribe(user => { this.user = user; });
  }

  ngOnInit(): void {
    this.loadMember();
  }

  loadMember() {
    this.memberService.getMemberByUsername(this.user.userName).subscribe(member => {
      this.member = member;
    });
  }

  updateMember() {
    console.log(this.member);
    this.memberService.memberUpdate(this.member).subscribe(() => {
      this.editForm.reset(this.member);
      this.toastr.success('Profile updated successfully');
    }, (err: any) => {
      console.log('Error Updating User');
      this.toastr.error(err);
    });
  }

}
