/**
 * Public API for the resource-engine. Everything the host app needs.
 */
export * from './lib/models/resource.models';
export * from './lib/services/resource-registry.service';
export * from './lib/services/permission.service';
export * from './lib/services/locale.service';
export * from './lib/guards/resource-exists.match';
export * from './lib/guards/bootstrap.resolver';
export * from './lib/pages/resource-list.page';
export * from './lib/pages/resource-detail.page';
export * from './lib/directives/has-permission.directive';
export * from './lib/components/dynamic-grid.component';
export * from './lib/components/dynamic-form.component';
