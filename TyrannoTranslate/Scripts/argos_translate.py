import sys
import io
import json
import re
import subprocess
import importlib.util
import os

sys.stdin = io.TextIOWrapper(sys.stdin.buffer, encoding="utf-8-sig")
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding="utf-8")

TAG_PATTERN = re.compile(r"\[[^\]]*\]")


def split_and_translate(text, source_lang, target_lang):
    """Split text on tags, translate each text segment separately,
    then interleave tags back. This prevents tags from confusing the model."""
    parts = []
    tags = []
    start = 0

    for m in TAG_PATTERN.finditer(text):
        if m.start() > start:
            parts.append(text[start : m.start()])
        tags.append(m.group())
        parts.append(None)
        start = m.end()

    if start < len(text):
        parts.append(text[start:])

    for i, part in enumerate(parts):
        if part is None:
            parts[i] = tags.pop(0)
        elif part.strip():
            parts[i] = argostranslate.translate.translate(
                part, source_lang, target_lang
            )
    return "".join(parts)


def ensure_package(package_name):
    spec = importlib.util.find_spec(package_name)
    if spec is None:
        subprocess.check_call(
            [sys.executable, "-m", "pip", "install", package_name],
            stdout=subprocess.DEVNULL,
            stderr=subprocess.DEVNULL,
        )


def main():
    global argostranslate
    try:
        input_raw = sys.stdin.read()
        if not input_raw:
            output = {"success": False, "error": "No input received from C#"}
            sys.stdout.write(json.dumps(output, ensure_ascii=False))
            sys.stdout.flush()
            return

        input_data = json.loads(input_raw)
        source_lang = input_data["source_lang"]
        target_lang = input_data["target_lang"]
        texts = input_data["texts"]
        packages_dir = input_data.get("packages_dir", "")

        if not texts:
            output = {"success": True, "results": []}
            sys.stdout.write(json.dumps(output, ensure_ascii=False))
            sys.stdout.flush()
            return

        if packages_dir:
            os.environ["ARGOS_PACKAGES_DIR"] = packages_dir

        ensure_package("argostranslate")

        import argostranslate.package
        import argostranslate.translate

        argostranslate.package.update_package_index()
        available_packages = argostranslate.package.get_available_packages()

        package = None
        for p in available_packages:
            if p.from_code == source_lang and p.to_code == target_lang:
                package = p
                break

        if package is None:
            raise ValueError(
                f"No translation package found for {source_lang} -> {target_lang}"
            )

        installed_packages = argostranslate.package.get_installed_packages()
        installed = any(
            pk.from_code == source_lang and pk.to_code == target_lang
            for pk in installed_packages
        )

        if not installed:
            download_path = package.download()
            argostranslate.package.install_from_path(download_path)

        results = []
        for text in texts:
            results.append(
                split_and_translate(text, source_lang, target_lang)
            )

        output = {"success": True, "results": results}
        sys.stdout.write(json.dumps(output, ensure_ascii=False))
        sys.stdout.flush()

    except Exception as e:
        error_msg = f"{type(e).__name__}: {e}"
        output = {"success": False, "error": error_msg}
        sys.stdout.write(json.dumps(output, ensure_ascii=False))
        sys.stdout.flush()


if __name__ == "__main__":
    main()
