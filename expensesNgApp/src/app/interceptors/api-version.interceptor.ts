import { HttpEventType, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { tap } from 'rxjs/operators';
import { VersionService } from '../services/version.service';

export const apiVersionInterceptor: HttpInterceptorFn = (req, next) => {
  const versionService = inject(VersionService);

  return next(req).pipe(
    tap(event => {
      if (event.type === HttpEventType.Response) {
        const apiVersion = event.headers.get('X-Api-Version');
        if (apiVersion) {
          versionService.checkApiVersion(apiVersion);
        }
      }
    })
  );
};
