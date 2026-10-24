import copy
import json
from pathlib import Path
from src.rule_validator import evaluate, load_yaml, validate_extraction
from jsonschema.exceptions import ValidationError
import pytest
BASE=Path(__file__).resolve().parents[1]
POLICY=load_yaml(BASE/'policies/demo_saas_policy.yaml')
RULES=load_yaml(BASE/'rules/cyber_rules.yaml')

def run(name):
    data=json.loads((BASE/'fixtures'/f'{name}.json').read_text(encoding='utf-8'))
    result=evaluate(data,POLICY,RULES)
    return {x['rule_id']:x for x in result['findings']}

def test_rule_catalog_has_twenty_unique_rules():
    ids=[r['id'] for r in RULES['rules']]
    assert len(ids)==20 and len(ids)==len(set(ids))

def test_pass():
    assert run('pass')['CY-001']['status']=='PASS'

def test_fail():
    assert run('fail')['CY-001']['status']=='FAIL'

def test_needs_review_on_missing_fact():
    assert run('needs_review')['CY-001']['status']=='NEEDS_REVIEW'

def test_not_applicable():
    assert run('not_applicable')['CY-001']['status']=='NOT_APPLICABLE'

def test_detect_explicit_missing_mfa_and_subcontractor_flowdown():
    result=run('mixed')
    assert result['CY-015']['status']=='FAIL'
    assert result['CY-002']['status']=='PASS'

def test_evidence_missing_requires_review():
    data=json.loads((BASE/'fixtures/fail.json').read_text(encoding='utf-8'))
    data['facts']['incident.notification_hours']['evidence']=[]
    res=evaluate(data,POLICY,RULES)
    assert next(x for x in res['findings'] if x['rule_id']=='CY-001')['status']=='NEEDS_REVIEW'

def test_schema_rejects_missing_tenant():
    data=json.loads((BASE/'fixtures/fail.json').read_text(encoding='utf-8'))
    del data['tenant_id']
    with pytest.raises(ValidationError):validate_extraction(data)

def test_rule_policy_coverage():
    for rule in RULES['rules']:
        assert rule['policy_key'] in POLICY['requirements']
