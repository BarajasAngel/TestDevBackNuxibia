import sys
import pandas as pd

def normalize_datetime_columns(df: pd.DataFrame) -> pd.DataFrame:
    # Mantén strings para que BULK INSERT sea estable (luego TRY_CONVERT en SQL).
    for col in df.columns:
        if pd.api.types.is_datetime64_any_dtype(df[col]):
            # ISO con milisegundos si existen (hasta 3 decimales)
            df[col] = df[col].dt.strftime("%Y-%m-%d %H:%M:%S.%f").str.slice(0, 23)
    return df

def main(xlsx_path: str, out_dir: str) -> None:
    xls = pd.ExcelFile(xlsx_path)
    for sheet in xls.sheet_names:
        df = pd.read_excel(xlsx_path, sheet_name=sheet)

        df = normalize_datetime_columns(df)

        # TSV (tab) para evitar problemas de comas/quotes en CSV.
        out_path = f"{out_dir}/{sheet}.tsv"
        df.to_csv(out_path, sep="\t", index=False, encoding="utf-8", lineterminator="\n")
        print(f"Written: {out_path} ({len(df)} rows)")

if __name__ == "__main__":
    if len(sys.argv) != 3:
        print("Usage: convert.py <input.xlsx> <out_dir>")
        sys.exit(2)
    main(sys.argv[1], sys.argv[2])
