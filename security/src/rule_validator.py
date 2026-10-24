"""Offline reference validator. NOT the production C# risk scoring engine."""
from pathlib import Path
import json
import yaml
from jsonschema import Draft202012Validator
BASE = Path(__file__).resolve().parents[1]

def load_yaml(path):
    return yaml.safe_load(Path(path).read_text(encoding='utf-8'))

def validate_extraction(data):
    schema = json.loads(
    (BASE.parent / "contracts" / "extraction.schema.json")
    .read_text(encoding="utf-8")
)
    Draft202012Validator(schema).validate(data)

def evaluate(data, policy, rule_set):
    validate_extraction(data)
    outputs = []
    for rule in rule_set['rules']:
        key = rule['fact_key']
        applies = rule.get('applicable_when', {})
        if any(data['context'].get(c) is None for c in applies):
            status, reason, evidence = 'NEEDS_REVIEW', 'Context unknown; applicability cannot be determined', []
        elif any(data['context'][c] != desired for c,desired in applies.items()):
            status, reason, evidence = 'NOT_APPLICABLE', 'Rule does not apply to this contract context', []
        else:
            f = data['facts'].get(key)
            evidence = f.get('evidence', []) if f else []
            if not f or f['status'] != 'FOUND':
                status, reason = 'NEEDS_REVIEW', 'Fact missing or extraction incomplete; human/document review required'
            elif rule.get('evidence_required') and not evidence:
                status, reason = 'NEEDS_REVIEW', 'Evidence missing; cannot auto-confirm compliance'
            elif rule['policy_key'] not in policy['requirements']:
                status, reason = 'NEEDS_REVIEW', 'Policy value not configured'
            elif type(f['value']) is bool and rule['operator'] == 'max':
                status, reason = 'NEEDS_REVIEW', 'Invalid number from model'
            else:
                expected = policy['requirements'][rule['policy_key']]
                actual = f['value']
                op = rule['operator']
                if op == 'eq': ok = type(actual) is type(expected) and actual == expected
                elif op == 'max': ok = isinstance(actual,(int,float)) and not isinstance(actual,bool) and isinstance(expected,(int,float)) and actual <= expected
                else: raise ValueError(f'Unsupported operator {op}')
                status = 'PASS' if ok else 'FAIL'
                reason = ('Value matches company policy' if ok else 'Confirmed mismatch against demo company policy')
        outputs.append({'rule_id':rule['id'],'domain':rule['domain'],'status':status,'severity':rule['severity_on_fail'] if status=='FAIL' else 'NONE','fact_key':key,'evidence':evidence,'reason':reason})
    result = dict(schema_version='1.0.0',job_id=data['job_id'],tenant_id=data['tenant_id'],contract_id=data['contract_id'],policy_id=policy['policy_id'],policy_version=policy['version'],rule_set_version=rule_set['schema_version'],findings=outputs)
    result_schema=json.loads((BASE/'schemas/security-findings.schema.json').read_text(encoding='utf-8'))
    Draft202012Validator(result_schema).validate(result)
    return result

def main():
    import argparse
    parser=argparse.ArgumentParser()
    parser.add_argument('--input',default=str(BASE/'fixtures/mixed.json'))
    parser.add_argument('--output',default=str(BASE/'out/security-findings.json'))
    args=parser.parse_args()
    data=json.loads(Path(args.input).read_text(encoding='utf-8'))
    out=evaluate(data,load_yaml(BASE/'policies/demo_saas_policy.yaml'),load_yaml(BASE/'rules/cyber_rules.yaml'))
    target=Path(args.output);target.parent.mkdir(parents=True,exist_ok=True)
    target.write_text(json.dumps(out,ensure_ascii=False,indent=2),encoding='utf-8')
    print('Written:', target)
    print({k:sum(f['status']==k for f in out['findings']) for k in ['PASS','FAIL','NOT_APPLICABLE','NEEDS_REVIEW']})
if __name__ == '__main__':main()
