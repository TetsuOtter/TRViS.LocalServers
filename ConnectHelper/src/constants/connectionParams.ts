const params = new URLSearchParams(window.location.search);
const hostParam = params.get("host") ?? "";
export const SERVER_HOSTS = hostParam.split(",").filter((h) => h.length > 0);
export const SERVER_PORT = parseInt(params.get("port") ?? "");
export const IS_URL_PARAM_ERROR =
	SERVER_HOSTS.length === 0 || !(0 < SERVER_PORT && SERVER_PORT < 65536);

export const getTimetableJsonUrl = (host: string) =>
	`http://${host}:${SERVER_PORT}/timetable.json`;
export const getSyncDataJsonUrl = (host: string) =>
	`http://${host}:${SERVER_PORT}/sync`;
export const getSyncDataWsUrl = (host: string) =>
	`ws://${host}:${SERVER_PORT}/ws`;
export const getTrvisAppLinkPath = (host: string) =>
	`trvis://app/open/json?path=${getTimetableJsonUrl(host)}&rts=${getSyncDataJsonUrl(host)}`;
export const getTrvisAppLinkWs = (host: string) =>
	`trvis://app/open/json?path=${getSyncDataWsUrl(host)}`;
export const TRVIS_APP_LINK_DATA = `trvis://app/open/json?data=`;
