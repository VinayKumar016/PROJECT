import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule, Router } from '@angular/router';
import Swal from 'sweetalert2';
import { CharityService } from '../services/charity.service';
import { DAFService } from '../services/daf.service';
import { DonationService } from '../services/donation.service';

@Component({
  selector: 'app-add-donation',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './add-donation.component.html',
  styleUrl: './add-donation.component.css'
})
export class AddDonationComponent implements OnInit {
  dafBalance = 0;
  totalDonated = 0;
  charities: any[] = [];
  donatedCharities: any[] = [];
  selectedCharity: any = null;
  showCharityList: boolean = false;

  donationAmount: number | null = null;
  donationDesc = '';
  agreedToTerms = false;
  searchTerm = '';

  userName = '';

  constructor(
    private charityService: CharityService,
    private dafService: DAFService,
    private donationService: DonationService,
    private router: Router
  ) {}

  ngOnInit(): void {
    const userId = localStorage.getItem('userId');
    this.userName = localStorage.getItem('userName') || '';

    this.loadDashboard(userId ? Number(userId) : 0);
    
  }
  logout() {
    localStorage.clear();
  }
  loadDashboard(userId: number){
    this.dafService.getDAFAccount(userId).subscribe(daf =>{
      this.dafBalance = daf.dafBalance;
      this.totalDonated = daf.totalDonated;
      console.log(daf);
        
      });
    this.charityService.getAllCharities().subscribe(data => {
      this.charities = data;
      console.log(this.charities);
      this.donatedCharities = data.filter(charity => charity.donated);
    });

    this.donationService.getUserDonations(userId).subscribe(
      donations=>{
        console.log(donations);
        const ids = new Set<number>();
        donations.forEach(d => ids.add(d.charityId));
        console.log(ids);
        
        this.donatedCharities = this.charities.filter(charity => ids.has(charity.charityId));
        console.log(this.donatedCharities);
      }
    );

  }

  hideCharityList() {
    setTimeout(() => {
      this.showCharityList = false; // Delay hiding to allow click events to register
    }, 200);
  }

  selectCharity(charity: any) {
    this.selectedCharity = charity;
    this.searchTerm = charity.name; // Update search bar with selected charity name
    this.showCharityList = false; // Hide the list after selection
  }

  donate() {
    if (!this.selectedCharity || !this.donationAmount || !this.agreedToTerms){
      Swal.fire('Error', 'Please fill in all fields and agree to the terms.', 'error');
      return;
    }

    if (this.donationAmount > this.dafBalance) {
      Swal.fire('Error', 'Insufficient DAF balance.', 'error');
      return;
    }

    const userId = Number(localStorage.getItem('userId'));

    this.donationService.addDonation({
      userId,
      charityId: this.selectedCharity.charityId,
      amount: this.donationAmount,
      description: this.donationDesc,
      
    }).subscribe(()=>{
      Swal.fire('Success', 'Donation Successful !','success');
      this.loadDashboard(userId);
      this.selectedCharity = null;
      this.donationAmount = null;
      this.donationDesc = '';
      this.agreedToTerms = false;

    });

    this.router.navigate(['/dashboard']);
  }
  get filteredCharities() {
    return this.charities.filter(c =>
      c.name.toLowerCase().includes(this.searchTerm.toLowerCase())
    );
  }
}
