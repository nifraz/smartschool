export type FieldType =
  | 'String' | 'Int' | 'Long' | 'Decimal' | 'Bool'
  | 'Date' | 'DateTime' | 'Enum' | 'Ref' | 'RefCollection' | 'Json';

export interface FieldDescriptor {
  key: string;
  label: string;
  type: FieldType;
  required: boolean;
  unique: boolean;
  inGrid: boolean;
  inForm: boolean;
  refResource?: string | null;
  enumName?: string | null;
  enumValues: string[];
  visibleWhen?: string | null;
  sortOrder: number;
}

export interface RelationDescriptor {
  key: string;
  targetResource: string;
  kind: 'one' | 'many';
  foreignKey: string;
  label?: string | null;
  showInDetail: boolean;
}

export interface ActionDescriptor {
  key: string;
  label: string;
  icon?: string | null;
  permission?: string | null;
  kind: 'row' | 'bulk' | 'global';
}

export interface ResourceDescriptor {
  key: string;
  plural: string;
  module: string;
  icon: string;
  label: string;
  isSystem: boolean;
  sortOrder: number;
  fields: FieldDescriptor[];
  relations: RelationDescriptor[];
  actions: ActionDescriptor[];
}

export interface NavNode {
  key: string;
  label: string;
  icon?: string | null;
  resourceKey?: string | null;
  path?: string | null;
  permissionCode?: string | null;
  sortOrder: number;
  children: NavNode[];
}
