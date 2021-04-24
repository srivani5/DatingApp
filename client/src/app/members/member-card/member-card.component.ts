import { Component, Input, OnInit } from '@angular/core';
import { ToastrService } from 'ngx-toastr';
import { Member } from 'src/app/_models/member';
import { MemberService } from 'src/app/_services/member.service';

@Component({
  selector: 'app-member-card',
  templateUrl: './member-card.component.html',
  styleUrls: ['./member-card.component.scss']
})
export class MemberCardComponent implements OnInit {
  @Input() member: Member;
  constructor(private memberService: MemberService, private toastr: ToastrService) { }

  ngOnInit(): void {
  }

  addLike(username){
    this.memberService.addLike(username).subscribe(() => {
      this.toastr.success('You liked ' + username);
    });
  }

}
