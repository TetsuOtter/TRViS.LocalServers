import { useState } from "react";

import "./App.css";
import CurrentData from "./Components/CurrentData";
import ConnectionQr from "./Components/ConnectionQr";
import {
	IS_URL_PARAM_ERROR,
	TIMETABLE_JSON_URL,
	TRVIS_APP_LINK_PATH,
	TRVIS_APP_LINK_WS,
} from "./constants/connectionParams";
import LicenseInfo from "./Components/LicenseInfo";
import ConnectionlessQr from "./Components/ConnectionlessQr";

const App = () => {
	const [hasCurrentDataLoadError, setHasCurrentDataLoadError] =
		useState<boolean>(false);
	const [isConnectionlessMode, setIsConnectionlessMode] =
		useState<boolean>(false);

	return (
		<>
			<h1>TRViS連携用QRコード</h1>
			{isConnectionlessMode ? (
				<ConnectionlessQr />
			) : (
				<ConnectionQr hasCurrentDataLoadError={hasCurrentDataLoadError} />
			)}

			{!IS_URL_PARAM_ERROR && (
				<>
					<p>
						ゲーム側でシナリオを読み込んだ後、TRViSがインストールされた端末でこのQRコードを読み取ってください。
						<br />
						(スマートフォンとこのマシンが同じネットワークに属している必要があります)
					</p>

					<p>
						または、下のURLをTRViSにて入力してください。(TRViS
						v0.1.0-85以降のみ)
						<br />
						<a href={TRVIS_APP_LINK_WS}>{TRVIS_APP_LINK_WS}</a>
					</p>

					<p>
						TRViS v0.1.0-74以降はこちら
						<br />
						<a href={TRVIS_APP_LINK_PATH}>{TRVIS_APP_LINK_PATH}</a>
					</p>

					<p>
						それ以前のTRViSではこちら
						<br />
						<a href={TIMETABLE_JSON_URL}>{TIMETABLE_JSON_URL}</a>
					</p>

					<p>
						QRコードを読んでもうまく接続できない場合は、通信不要モードをお試しください。
						<br />
						<button
							onClick={() => setIsConnectionlessMode((v) => !v)}
							style={{
								margin: "0.5em",
								padding: "0.5em",
							}}>
							{isConnectionlessMode
								? "通信モードに切り替え"
								: "通信不要モードに切り替え"}
						</button>
					</p>
				</>
			)}

			{!IS_URL_PARAM_ERROR && (
				<CurrentData setHasError={setHasCurrentDataLoadError} />
			)}

			<LicenseInfo />
		</>
	);
};

export default App;
