import { useState } from "react";

import "./App.css";
import CurrentData from "./Components/CurrentData";
import ConnectionQr from "./Components/ConnectionQr";
import {
	IS_URL_PARAM_ERROR,
	TIMETABLE_JSON_URL,
} from "./constants/connectionParams";
import LicenseInfo from "./Components/LicenseInfo";

const App = () => {
	const [hasCurrentDataLoadError, setHasCurrentDataLoadError] =
		useState<boolean>(false);

	return (
		<>
			<h1>TRViS連携用QRコード</h1>
			<ConnectionQr hasCurrentDataLoadError={hasCurrentDataLoadError} />

			{!IS_URL_PARAM_ERROR && (
				<>
					<p>
						ゲーム側でシナリオを読み込んだ後、TRViSがインストールされた端末でこのQRコードを読み取ってください。
						<br />
						(スマートフォンとこのマシンが同じネットワークに属している必要があります)
					</p>

					<p>
						または、下のURLをTRViSにて入力してください。
						<br />
						<a href={TIMETABLE_JSON_URL}>{TIMETABLE_JSON_URL}</a>
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
