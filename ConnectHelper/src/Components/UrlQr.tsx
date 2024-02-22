import {
	CSSProperties,
	Dispatch,
	SetStateAction,
	Suspense,
	useEffect,
	useState,
} from "react";
import QRCode from "qrcode";
import { ErrorBoundary } from "react-error-boundary";

export type UrlQrProps = {
	url: string;
	style?: CSSProperties;
	errorCorrectionLevel?: QRCode.QRCodeToDataURLOptions["errorCorrectionLevel"];
};
type LazyUrlQrProps = UrlQrProps & {
	dataUrl: string | null;
	setDataUrl: Dispatch<SetStateAction<string | null>>;
};

const qrOptions: QRCode.QRCodeToDataURLOptions = {
	errorCorrectionLevel: "H",
	margin: 0,
	width: 512,
};

const LazyUrlQr = ({
	url,
	style,
	errorCorrectionLevel,
	dataUrl,
	setDataUrl,
}: LazyUrlQrProps) => {
	if (dataUrl != null) {
		return (
			<img
				src={dataUrl}
				style={style}
			/>
		);
	}

	throw QRCode.toDataURL(url, {
		...qrOptions,
		errorCorrectionLevel:
			errorCorrectionLevel ?? qrOptions.errorCorrectionLevel,
	}).then((_dataUrl) => {
		setDataUrl(_dataUrl);
		return (
			<img
				src={_dataUrl}
				style={style}
			/>
		);
	});
};

const UrlQr = (props: UrlQrProps) => {
	const [dataUrl, setDataUrl] = useState<string | null>(null);

	useEffect(() => {
		setDataUrl(null);
	}, [props.url]);

	return (
		<ErrorBoundary fallback={<div>QRコードの生成に失敗しました</div>}>
			<Suspense fallback={<p>QRコードを生成中...</p>}>
				<LazyUrlQr
					{...props}
					dataUrl={dataUrl}
					setDataUrl={setDataUrl}
				/>
			</Suspense>
		</ErrorBoundary>
	);
};

export default UrlQr;
