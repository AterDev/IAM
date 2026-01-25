// <auto-generate>
import { Injectable, Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'enumText'
})
@Injectable({ providedIn: 'root' })
export class EnumTextPipe implements PipeTransform {
  transform(value: unknown, type: string): string {
    let result = '';
    switch (type) {
      case 'ApplicationType':
        switch (value) {
          case 0: result = 'Web'; break;
          case 1: result = 'Native'; break;
          case 2: result = 'Spa'; break;
          default: result = '默认'; break;
        }
        break;

      case 'ClientType':
        switch (value) {
          case 0: result = 'Confidential'; break;
          case 1: result = 'Public'; break;
          default: result = '默认'; break;
        }
        break;

      case 'ConsentType':
        switch (value) {
          case 0: result = 'Explicit'; break;
          case 1: result = 'Implicit'; break;
          case 2: result = 'Systematic'; break;
          default: result = '默认'; break;
        }
        break;


      default:
        break;
    }
    return result;
  }
}
