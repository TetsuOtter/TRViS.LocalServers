import { useState } from "react";

import "./App.css";
import CurrentData from "./Components/CurrentData";
import ConnectionQr from "./Components/ConnectionQr";
import {
	IS_URL_PARAM_ERROR,
	SERVER_HOSTS,
	getTimetableJsonUrl,
	getTrvisAppLinkPath,
	getTrvisAppLinkWs,
} from "./constants/connectionParams";
import LicenseInfo from "./Components/LicenseInfo";
import ConnectionlessQr from "./Components/ConnectionlessQr";

const App = () => {
	const [hasCurrentDataLoadError, setHasCurrentDataLoadError] =
		useState<boolean>(false);
	const [isConnectionlessMode, setIsConnectionlessMode] =
		useState<boolean>(false);
	const [selectedHost, setSelectedHost] = useState<string>(
		SERVER_HOSTS[0] ?? ""
	);

	const timetableJsonUrl = getTimetableJsonUrl(selectedHost);
	const trvisAppLinkPath = getTrvisAppLinkPath(selectedHost);
	const trvisAppLinkWs = getTrvisAppLinkWs(selectedHost);

	return (
		<>
			<h1>TRViS連携用QRコード</h1>
			{isConnectionlessMode ? (
				<ConnectionlessQr selectedHost={selectedHost} />
			) : (
				<ConnectionQr
					hasCurrentDataLoadError={hasCurrentDataLoadError}
					selectedHost={selectedHost}
				/>
			)}

			{!IS_URL_PARAM_ERROR && (
				<>
					{SERVER_HOSTS.length > 1 && (
						<p>
							<label htmlFor="host-selector">接続先IPアドレス: </label>
							<select
								id="host-selector"
								value={selectedHost}
								onChange={(e) => setSelectedHost(e.target.value)}
								style={{
									padding: "0.5em",
									fontSize: "1em",
								}}>
								{SERVER_HOSTS.map((host) => (
									<option key={host} value={host}>
										{host}
									</option>
								))}
							</select>
						</p>
					)}

					<p>
						ゲーム側でシナリオを読み込んだ後、TRViSがインストールされた端末でこのQRコードを読み取ってください。
						<br />
						(スマートフォンとこのマシンが同じネットワークに属している必要があります)
					</p>

					<p>
						または、下のURLをTRViSにて入力してください。(TRViS
						v0.1.0-85以降のみ)
						<br />
						<a href={trvisAppLinkWs}>{trvisAppLinkWs}</a>
					</p>

					<p>
						TRViS v0.1.0-74以降はこちら
						<br />
						<a href={trvisAppLinkPath}>{trvisAppLinkPath}</a>
					</p>

					<p>
						それ以前のTRViSではこちら
						<br />
						<a href={timetableJsonUrl}>{timetableJsonUrl}</a>
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
				<CurrentData
					setHasError={setHasCurrentDataLoadError}
					selectedHost={selectedHost}
				/>
			)}

			<LicenseInfo />
		</>
	);
};

export default App;
