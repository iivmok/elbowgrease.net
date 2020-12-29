import { call, select } from "redux-saga/effects";

export interface CallOptions {
  body?: any;
  anonymous?: boolean;
  method?: string;
}
export function* apiCall(path: string, options: CallOptions = {}) {
  let url = path;
  options = {
    body: null,
    anonymous: false,
    method: "GET",
    ...options,
  };
  let headers = {
    Accept: "application/json",
  };

  if (!options.anonymous) {
    const token = yield select();
    headers["Authorization"] = "Bearer ".concat(token);
  }

  if (options.method !== "GET" && !(options.body instanceof FormData)) {
    options.body = JSON.stringify(options.body);
    headers["Content-type"] = "application/json";
  }

  const reqOptions: RequestInit = {
    body: options.body,
    headers: new Headers(headers),
    method: options.method,
  };
  
  return yield call(window.fetch, url, reqOptions);
}
