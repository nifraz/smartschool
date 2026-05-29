import { Injectable, computed, inject, signal } from '@angular/core';
import { Apollo, gql } from 'apollo-angular';
import { firstValueFrom } from 'rxjs';
import { NavNode, ResourceDescriptor } from '../models/resource.models';

const RESOURCES_QUERY = gql`
  query Resources {
    resources {
      key plural module icon label isSystem sortOrder
      fields {
        key label type required unique inGrid inForm
        refResource enumName enumValues visibleWhen sortOrder
      }
      relations { key targetResource kind foreignKey label showInDetail }
      actions   { key label icon permission kind }
    }
    navigation {
      key label icon resourceKey path permissionCode sortOrder
      children {
        key label icon resourceKey path permissionCode sortOrder
        children { key label icon resourceKey path permissionCode sortOrder children { key } }
      }
    }
  }
`;

@Injectable({ providedIn: 'root' })
export class ResourceRegistryService {
  private readonly apollo = inject(Apollo);

  private readonly _resources = signal<ResourceDescriptor[]>([]);
  private readonly _navigation = signal<NavNode[]>([]);

  readonly resources = this._resources.asReadonly();
  readonly navigation = this._navigation.asReadonly();
  readonly resourceMap = computed(() =>
    new Map(this._resources().map(r => [r.key, r])));

  async load(): Promise<void> {
    const res = await firstValueFrom(this.apollo.query<{
      resources: ResourceDescriptor[];
      navigation: NavNode[];
    }>({ query: RESOURCES_QUERY, fetchPolicy: 'network-only' }));
    this._resources.set(res.data.resources ?? []);
    this._navigation.set(res.data.navigation ?? []);
  }

  get(key: string): ResourceDescriptor | undefined {
    return this.resourceMap().get(key);
  }

  /** Resolve a resource by its plural URL segment (e.g. "schools" → School). */
  getByPlural(plural: string): ResourceDescriptor | undefined {
    return this._resources().find(r => r.plural === plural || r.key === plural);
  }
}
