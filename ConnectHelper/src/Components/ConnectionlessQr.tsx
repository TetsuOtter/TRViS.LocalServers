import { memo, useCallback, useEffect, useState } from "react";
import {
	TIMETABLE_JSON_URL,
	TRVIS_APP_LINK_DATA,
} from "../constants/connectionParams";
import UrlQr from "./UrlQr";

export default memo(function ConnectionlessQr() {
	const [timetableJsonBase64, setTimetableJsonBase64] = useState<string | null>(
		null
	);
	const [errorMessage, setErrorMessage] = useState<string | null>(null);

	const onReloadClicked = useCallback(async () => {
		try {
			setErrorMessage(null);

			const res = await fetch(TIMETABLE_JSON_URL);

			if (res.status !== 200 || res.body == null) {
				throw new Error("時刻表データを読み込めませんでした");
			}

			const arrayBuffer = await res.arrayBuffer();
			if (arrayBuffer.byteLength === 0) {
				throw new Error("時刻表データが空です");
			}

			const dataBase64 = btoa(
				String.fromCharCode(...new Uint8Array(arrayBuffer))
			);
			if (dataBase64 === "") {
				throw new Error("時刻表データの変換に失敗しました");
			}

			const dataBase64Url = dataBase64
				.replace(/\+/g, "-")
				.replace(/\//g, "_")
				.replace(/=+$/, "");
			setTimetableJsonBase64(dataBase64Url);
		} catch (ex) {
			setTimetableJsonBase64(null);
			console.error("failed to fetch", ex);
			const alertMessage = ex instanceof Error ? ex.message : "不明なエラー";
			setErrorMessage("時刻表データの読み込みに失敗しました: " + alertMessage);
			return;
		}
	}, []);

	useEffect(() => {
		onReloadClicked();
	}, [onReloadClicked]);

	const appLink = TRVIS_APP_LINK_DATA + timetableJsonBase64;

	return (
		<div
			style={{
				margin: "0 auto",
				padding: "0.5em",
				width: "90vmin",
				border: "solid 1px black",
			}}>
			<button onClick={onReloadClicked}>再読み込み</button>
			<p style={{ color: "red" }}>{errorMessage}</p>
			{timetableJsonBase64 != null && (
				<UrlQr
					url={appLink}
					style={{
						width: "80%",
						margin: "10%",
					}}
					errorCorrectionLevel="L"
				/>
			)}
			{timetableJsonBase64 != null && (
				<>
					<p>
						2~3秒待ってもQRコードがうまく表示されない、または端末でQRコードを読み取れない場合は、TRViSをインストールした端末のブラウザで下のURLを開いてください。
						<br />
					</p>
					<details>
						<summary>TRViSを開くURL (クリックで展開します)</summary>
						<pre>
							<code
								style={{
									textWrap: "wrap",
									overflowWrap: "anywhere",
								}}>
								{appLink}
							</code>
						</pre>
					</details>
					<p>
						<a href={appLink}>
							(このリンクを右クリックして「リンクをコピー」を選択しても、URLをコピーできます)
						</a>
					</p>
				</>
			)}
		</div>
	);
});
