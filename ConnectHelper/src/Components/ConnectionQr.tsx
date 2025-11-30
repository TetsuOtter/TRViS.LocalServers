import { memo, useState } from "react";
import UrlQr from "./UrlQr";
import {
	IS_URL_PARAM_ERROR,
	SERVER_HOST,
	SERVER_PORT,
	TRVIS_APP_LINK_PATH,
	TRVIS_APP_LINK_WS,
} from "../constants/connectionParams";

type ConnectionQrProps = {
	hasCurrentDataLoadError: boolean;
};

type LinkType = "http" | "websocket";

const BUTTON_CONTAINER_STYLE = {
	marginBottom: "1em",
	display: "flex" as const,
	justifyContent: "center" as const,
	gap: "0.5em",
};

const getButtonStyle = (isSelected: boolean) =>
	({
		padding: "0.5em 1em",
		fontWeight: isSelected ? "bold" : "normal",
		cursor: "pointer" as const,
		backgroundColor: isSelected ? "#007bff" : "#f0f0f0",
		color: isSelected ? "#fff" : "#333",
		border: isSelected ? "2px solid #0056b3" : "2px solid #ccc",
		borderRadius: "4px",
	}) as const;

const QR_CONTAINER_STYLE = {
	width: "80%",
	margin: "10%",
};

export default memo(function ConnectionQr({
	hasCurrentDataLoadError,
}: ConnectionQrProps) {
	const [forceShowQr, setForceShowQr] = useState<boolean>(false);
	const [linkType, setLinkType] = useState<LinkType>("websocket");

	return (
		<div
			style={{
				margin: "0 auto",
				padding: "0.5em",
				width: "90vmin",
				border: "solid 1px black",
			}}>
			{IS_URL_PARAM_ERROR ? (
				<span style={{ color: "red", fontWeight: "bold" }}>
					パラメータが不正です
					<br />
					(host: {SERVER_HOST}, port: {SERVER_PORT})
				</span>
			) : !forceShowQr && hasCurrentDataLoadError ? (
				<>
					<p>
						シナリオが読み込まれていない、または時刻表サーバと接続できないため、
						<br />
						QRコードを非表示にしています。
					</p>
					<p>
						シナリオ読み込み前は時刻表データが存在しないため、
						<br />
						QRコードを読み込んでも
						<b> TRViSでデータの読み込みに失敗します</b>。
					</p>
					<p>
						それでもQRコードを表示したい場合は、下のボタンを押下してください。
						<br />
						<button
							style={{
								margin: "0.5em",
								padding: "0.5em",
							}}
							onClick={() => setForceShowQr(true)}>
							強制的にQRコードを表示
						</button>
					</p>
				</>
			) : (
				<>
					<UrlQr
						url={linkType === "http" ? TRVIS_APP_LINK_PATH : TRVIS_APP_LINK_WS}
						style={QR_CONTAINER_STYLE}
					/>
					<div style={BUTTON_CONTAINER_STYLE}>
						<button
							onClick={() => setLinkType("websocket")}
							style={getButtonStyle(linkType === "websocket")}>
							WebSocketリンク
						</button>
						<button
							onClick={() => setLinkType("http")}
							style={getButtonStyle(linkType === "http")}>
							HTTPリンク
						</button>
					</div>
				</>
			)}
		</div>
	);
});
