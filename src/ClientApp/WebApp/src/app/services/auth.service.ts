import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  isLogin = false;
  isAdmin = false;
  userName?: string | null = null;
  id?: string | null = null;
  constructor() {
    this.updateUserLoginState();
  }

  saveLoginState(username: string, token: string): void {
    this.isLogin = true;
    this.userName = username;
    localStorage.setItem("username", username);
    localStorage.setItem("accessToken", token);
  }

  updateUserLoginState(): void {
    const username = localStorage.getItem('username');
    const token = localStorage.getItem('accessToken');
    if (token && username) {
      this.userName = username;
      this.isLogin = true;
    } else {
      this.isLogin = false;
    }
  }

  isAuthenticated(): boolean {
    return this.isLogin;
  }

  user(): { preferred_username?: string; name?: string } | null {
    if (this.isLogin && this.userName) {
      return {
        preferred_username: this.userName,
        name: this.userName
      };
    }
    return null;
  }

  logout(): void {
    localStorage.clear();
    this.isLogin = false;
  }
}
