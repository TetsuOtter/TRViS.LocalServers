import { memo, useState } from "react";
import UrlQr from "./UrlQr";
import {
	IS_URL_PARAM_ERROR,
	SERVER_HOST,
	SERVER_PORT,
	TRVIS_APP_LINK_PATH,
} from "../constants/connectionParams";

type ConnectionQrProps = {
	hasCurrentDataLoadError: boolean;
};

export default memo(function ConnectionQr({
	hasCurrentDataLoadError,
}: ConnectionQrProps) {
	const [forceShowQr, setForceShowQr] = useState<boolean>(false);

	return (
		<div
			style={{
				margin: "0 auto",
				padding: "0.5em",
				width: "75vmin",
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
				<UrlQr
					url={TRVIS_APP_LINK_PATH}
					style={{
						width: "100%",
					}}
				/>
			)}
		</div>
	);
});
