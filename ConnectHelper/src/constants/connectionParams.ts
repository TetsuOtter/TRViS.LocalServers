const params = new URLSearchParams(window.location.search);
export const SERVER_HOST = params.get("host") ?? "";
export const SERVER_PORT = parseInt(params.get("port") ?? "");
export const IS_URL_PARAM_ERROR =
	SERVER_HOST === "" || !(0 < SERVER_PORT && SERVER_PORT < 65536);
export const TIMETABLE_JSON_URL = `http://${SERVER_HOST}:${SERVER_PORT}/timetable.json`;
export const TRVIS_APP_LINK_PATH = `trvis://app/open/json?path=${TIMETABLE_JSON_URL}`;
