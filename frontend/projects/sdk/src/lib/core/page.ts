export interface PageDto<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
}

export interface ListParams {
  page: number;
  sort?: string;
  dir?: 'asc' | 'desc';
  q?: string;
  filter?: string;
}
