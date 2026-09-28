import contextlib
import copy
import io
import json
from pathlib import Path
import tempfile
import unittest
import astra


class ContractTests(unittest.TestCase):
    def setUp(self):
        self.spec = astra.read_json(Path(__file__).parent / "examples/question.performance.json")

    def test_example_schema(self):
        astra.check_spec(self.spec)

    def test_wire_roundtrip(self):
        self.assertEqual(astra.convert_spec(astra.convert_spec(self.spec, True), False), self.spec)

    def test_unknown_field_rejected(self):
        self.spec["units"][0]["actor"] = "typo"
        with self.assertRaises(astra.CliError):
            astra.check_spec(self.spec)

    def test_missing_field_rejected(self):
        del self.spec["nodes"][0]["id"]
        with self.assertRaises(astra.CliError):
            astra.check_spec(self.spec)

    def test_string_enum_required(self):
        self.spec["nodes"][0]["kind"] = 0
        with self.assertRaises(astra.CliError):
            astra.check_spec(self.spec)

    def test_nonfinite_rejected(self):
        self.spec["units"][0]["duration"] = float("nan")
        with self.assertRaises(astra.CliError):
            astra.check_spec(self.spec)

    def test_negative_start_rejected(self):
        self.spec["units"][0]["actions"][0]["start"] = -1
        with self.assertRaises(astra.CliError):
            astra.check_spec(self.spec)

    def test_integer_overflow_rejected(self):
        self.spec["initialPatience"] = 2 ** 32
        with self.assertRaises(astra.CliError):
            astra.check_spec(self.spec)

    def test_unity_float_overflow_rejected(self):
        self.spec["units"][0]["duration"] = 1e100
        with self.assertRaises(astra.CliError):
            astra.check_spec(self.spec)

    def test_boolean_not_integer(self):
        self.spec["initialPatience"] = True
        with self.assertRaises(astra.CliError):
            astra.check_spec(self.spec)

    def test_duplicate_json_key(self):
        with tempfile.TemporaryDirectory() as folder:
            file = Path(folder) / "bad.json"
            file.write_text('{"a":1,"a":2}', encoding="utf-8")
            with self.assertRaises(astra.CliError):
                astra.read_json(file)

    def test_utf8_and_no_overwrite(self):
        with tempfile.TemporaryDirectory() as folder:
            file = Path(folder) / "演出.json"
            astra.write_json(file, {"台词": "这里是前哨站。"})
            self.assertEqual(astra.read_json(file)["台词"], "这里是前哨站。")
            with self.assertRaises(FileExistsError):
                astra.write_json(file, {})

    def test_usage_is_json_nonzero(self):
        output = io.StringIO()
        with contextlib.redirect_stdout(output):
            code = astra.main(["apply"])
        self.assertEqual(code, 2)
        self.assertEqual(json.loads(output.getvalue())["code"], "USAGE")

    def test_nan_timeout(self):
        output = io.StringIO()
        with contextlib.redirect_stdout(output):
            code = astra.main(["--timeout", "nan", "doctor"])
        self.assertEqual(code, 2)


if __name__ == "__main__":
    unittest.main()
