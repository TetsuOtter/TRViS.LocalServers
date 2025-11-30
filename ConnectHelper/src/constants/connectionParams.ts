const params = new URLSearchParams(window.location.search);
export const SERVER_HOST = params.get("host") ?? "";
export const SERVER_PORT = parseInt(params.get("port") ?? "");
export const IS_URL_PARAM_ERROR =
	SERVER_HOST === "" || !(0 < SERVER_PORT && SERVER_PORT < 65536);
export const TIMETABLE_JSON_URL = `http://${SERVER_HOST}:${SERVER_PORT}/timetable.json`;
export const SYNC_DATA_JSON_URL = `http://${SERVER_HOST}:${SERVER_PORT}/sync`;
export const SYNC_DATA_WS_URL = `ws://${SERVER_HOST}:${SERVER_PORT}/ws`;
export const TRVIS_APP_LINK_PATH = `trvis://app/open/json?path=${TIMETABLE_JSON_URL}&rts=${SYNC_DATA_JSON_URL}`;
export const TRVIS_APP_LINK_WS = `trvis://app/open/json?path=${SYNC_DATA_WS_URL}`;
export const TRVIS_APP_LINK_DATA = `trvis://app/open/json?data=`;
