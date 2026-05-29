import { inject } from '@angular/core';
import { ResolveFn } from '@angular/router';
import { Apollo, gql } from 'apollo-angular';
import { firstValueFrom } from 'rxjs';
import { LocaleService } from '../services/locale.service';
import { PermissionService } from '../services/permission.service';
import { ResourceRegistryService } from '../services/resource-registry.service';

const BOOTSTRAP_QUERY = gql`
  query Bootstrap($locale: String!) {
    locales { code name nativeName isRtl sortOrder }
    translations(locale: $locale) { items { namespace key value } }
  }
`;

/**
 * Loads everything required to render the dynamic UI:
 *   • resource descriptors (cached for app lifetime)
 *   • permissions
 *   • locales + initial translation bundle
 */
export const bootstrapResolver: ResolveFn<boolean> = async () => {
  const registry = inject(ResourceRegistryService);
  const locales = inject(LocaleService);
  const perms = inject(PermissionService);
  const apollo = inject(Apollo);

  if (registry.resources().length === 0) {
    await registry.load();
  }

  const locale = locales.current();
  const { data } = await firstValueFrom(apollo.query<{
    locales: any[];
    translations: { items: { namespace: string; key: string; value: string }[] };
  }>({ query: BOOTSTRAP_QUERY, variables: { locale }, fetchPolicy: 'network-only' }));

  locales.setAvailable(data.locales ?? []);

  // TODO: read permission codes from JWT or a dedicated query.
  perms.set(['*:*']);

  // Hand translations to ngx-translate by namespace.
  // Simplified: flat key map for the common namespace.
  const flat: Record<string, string> = {};
  for (const t of data.translations?.items ?? []) flat[t.key] = t.value;
  const { TranslateService } = await import('@ngx-translate/core');
  inject(TranslateService).setTranslation(locale, flat, true);

  return true;
};
