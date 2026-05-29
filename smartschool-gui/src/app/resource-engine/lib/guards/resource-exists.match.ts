import { inject } from '@angular/core';
import { CanMatchFn, Route, UrlSegment } from '@angular/router';
import { ResourceRegistryService } from '../services/resource-registry.service';

/**
 * Allows `/:resource` and `/:resource/:id` to match only when the segment
 * is a known resource key/plural. Unknown segments fall through to 404.
 */
export const resourceExistsMatch: CanMatchFn = (_route: Route, segments: UrlSegment[]) => {
  if (segments.length === 0) return false;
  const registry = inject(ResourceRegistryService);
  const resource = segments[0].path;
  return registry.getByPlural(resource) !== undefined
      || registry.get(resource) !== undefined;
};
