# Elbowgrease.NET

Generate TypeScript models and `redux-saga` API calls from C# classes and MVC actions.

For usage see [`test/Program.cs`](./test/Program.cs) and [`api.ts`](./test/bin/Debug/net5.0/api.ts)

### Generated result

`backend.ts`
```ts
import { call } from 'redux-saga/effects';
import { apiCall } from './api';
import {
  IOtherType,
  ITestType,
} from "./models";

export type DateString = string;

export let Backend = {
  Foo: {
    *Bar(test: ITestType, foobar: string) {
      return (yield call(apiCall, `/api/Foo/Bar/${foobar}`, {
        body: test,
        method: "POST",
      })) as IOtherType;
    },
    *Baz(foobar: number) {
      return (yield call(apiCall, `/api/Foo/Baz/${foobar}`, {
        anonymous: true,
      })) as ITestType[];
    },
    *File(files: FormData, dt: DateString) {
      yield call(apiCall, `/api/Foo/File/${dt}`, {
        anonymous: true,
        body: files,
        method: "POST",
      });
    },
  },
  URL: {
    Foo: {
      Bar(test: ITestType, foobar: string) {
        return `/api/Foo/Bar/${foobar}`;
      },
      Baz(foobar: number) {
        return `/api/Foo/Baz/${foobar}`;
      },
      File(files: FormData, dt: DateString) {
        return `/api/Foo/File/${dt}`;
      },
    },
  },
};
```
`models.ts`

```ts
export type DateString = string;

// test.OtherType
export interface IOtherType {
  str: string;
  int: number;
  list: string[];
  iList: string[];
  iEnum: string[];
  dateTime: DateString;
  nDateTime?: DateString | null;
  enum: EnumType;
  testType: ITestType;
  bool: boolean;
  char: string;
  weird?: boolean[] | null;
  charArray: string[];
  intArray: number[];
  dateArr: DateString[];
  nullableArr?: number[] | null;
}

// test.EnumType
export enum EnumType {
  foo = 0,
  bar = 1,
  baz = 12334,
}

// test.TestType
export interface ITestType {
  str: string;
  int: number;
  list: string[];
  iList: string[];
  iEnum: string[];
  dateTime: DateString;
  nDateTime?: DateString | null;
  enum: EnumType;
  otherType: IOtherType;
  bool: boolean;
  char: string;
  weird?: boolean[] | null;
  charArray: string[];
  intArray: number[];
  dateArr: DateString[];
  nullableArr?: number[] | null;
}
```