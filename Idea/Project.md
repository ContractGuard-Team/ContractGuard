Vậy sẽ **chốt lại hoàn toàn Project 2** theo hướng mới: không còn đặt behavioral monitoring làm trung tâm. Core của hệ thống là **AI đánh giá toàn diện rủi ro trong hợp đồng B2B**, còn Cybersecurity trở thành một nhóm risk lớn và một lớp bảo vệ riêng của toàn nền tảng.

# **ContractGuard**

### **AI-Powered B2B Contract Risk Intelligence Platform**

Mục tiêu cuối cùng:

> **Doanh nghiệp upload một hợp đồng với doanh nghiệp khác → AI hiểu cấu trúc hợp đồng → phân tích từng điều khoản → xác định các loại rủi ro → giải thích tại sao → đánh giá severity → đề xuất cách giảm rủi ro → tạo Risk Score cho từng phần và toàn bộ hợp đồng.**

Hệ thống không nên tuyên bố một điều khoản là “trái pháp luật” một cách tuyệt đối, vì luật phụ thuộc quốc gia, loại giao dịch và hoàn cảnh. Output đúng nên là **potential legal/compliance risk requiring review**.

---

# **1\. ContractGuard sẽ phân tích những rủi ro nào?**

Tôi đề xuất chia hợp đồng thành **7 Risk Domains**.

| Risk Domain | AI kiểm tra |
| ----- | ----- |
| **Legal Risk** | termination, liability, indemnity, dispute, governing law |
| **Financial Risk** | giá, payment term, penalty, advance payment, hidden cost |
| **Operational Risk** | SLA, delivery, warranty, responsibilities, dependency |
| **Compliance Risk** | regulatory obligation, records, audit, privacy |
| **Cybersecurity Risk** | breach, encryption, access, security obligations, incident notification |
| **Data & Privacy Risk** | ownership, processing, retention, deletion, transfer |
| **Third-Party/Vendor Risk** | subcontractor, dependency, audit rights, vendor obligations |

Trong đó tôi sẽ cố tình làm **Cybersecurity \+ Data Privacy \+ Third-Party Security rất sâu**, để role ATTT có giá trị thực sự.

---

# **2\. Luồng chính của hệ thống**

B2B Contract  
PDF / DOCX  
       ↓  
Secure Upload Gateway  
       ↓  
Document Parsing  
       ↓  
Contract Structure Detection  
       ↓  
Clause Segmentation  
       ↓  
Entity Extraction  
       ↓  
Clause Classification  
       ↓  
Risk Analysis  
       ↓  
Policy / Standard / Knowledge Comparison  
       ↓  
Clause Risk Score  
       ↓  
Contract Risk Aggregation  
       ↓  
AI Explanation  
       ↓  
Recommended Mitigation  
       ↓  
Risk Dashboard  
       ↓  
Review / Export Report

Đây sẽ là luồng end-to-end của project.

---

# **3\. Ví dụ đúng với sản phẩm bạn muốn**

User upload:

Service\_Agreement\_ABC.pdf

AI chia được:

1\. Payment  
2\. Termination  
3\. Liability  
4\. Confidentiality  
5\. Data Processing  
6\. Security  
7\. Service Level  
8\. Subcontracting  
9\. Governing Law

Sau đó hệ thống trả:

| Clause | Risk | Severity |
| ----- | ----- | ----- |
| Payment | 80% upfront payment | 🔴 High |
| Termination | No termination for convenience | 🟠 Medium |
| Liability | Unlimited liability | 🔴 Critical |
| Confidentiality | Acceptable | 🟢 Low |
| Data Processing | No deletion requirement | 🔴 High |
| Cybersecurity | No breach notification deadline | 🔴 Critical |
| SLA | 99% uptime only | 🟠 Medium |
| Subcontracting | Unlimited subcontracting | 🔴 High |

Cuối cùng:

Overall Contract Risk

Legal           72/100 HIGH  
Financial       67/100 MEDIUM-HIGH  
Operational     48/100 MEDIUM  
Compliance      74/100 HIGH  
Cybersecurity   91/100 CRITICAL  
Privacy         83/100 HIGH  
Third Party     76/100 HIGH

Overall:  
78 / 100 — HIGH RISK

Đây chính là sản phẩm mà tôi nghĩ bạn đang hướng tới.

---

# **4\. AI không chỉ nói "High Risk"**

Mỗi finding phải có cấu trúc chuẩn:

Finding:  
No defined security incident notification period.

Reference:  
Based on regulations identifying high-risk areas to avoid AI errors.

Clause:  
"The Provider shall notify the Customer  
of material security incidents."

Risk:  
HIGH

Why:  
The agreement does not specify how quickly  
the supplier must notify the customer.

Potential Impact:  
Late notification may delay incident response,  
containment and regulatory assessment.

Recommendation:  
Define a specific notification period  
and escalation procedure.

Category:  
Cybersecurity / Incident Response

Đây là điểm khiến project trông giống **Risk Intelligence Platform** hơn chatbot.

---

# **5\. Kiến trúc AI nên có 3 AI Engine**

Đội hình 3 AI của bạn rất phù hợp nếu chia như sau.

## **AI Member 1 — Contract Understanding Engineer**

Sở hữu:

> **Document Intelligence Engine**

Pipeline:

PDF  
 ↓  
Text Extraction  
 ↓  
Layout Understanding  
 ↓  
Clause Segmentation  
 ↓  
Clause Classification  
 ↓  
Entity Extraction

Extract:

Parties  
Contract type  
Contract value  
Effective date  
Expiration date  
Payment terms  
Jurisdiction  
Vendor  
Service  
SLA  
Termination  
Liability  
Security clauses

Ví dụ:

Clause \#17

Type:  
Termination

Text:  
...

Entities:  
Notice Period \= 30 days  
Party \= Customer

AI 1 nghiên cứu:

NLP  
LLM  
Embeddings  
Document parsing  
Clause classification  
NER  
Semantic similarity

**Deliverable chính:**

> Contract → structured JSON.

---

# **6\. AI Member 2 — Contract Risk Intelligence Engineer**

Member này nhận output của AI 1\.

Nhiệm vụ:

> **Đánh giá rủi ro của từng clause.**

Ví dụ AI 1:

Clause:  
Liability

Liability Cap:  
None

AI 2:

Risk:  
Unlimited Liability

Severity:  
CRITICAL

AI 2 phải kết hợp:

LLM  
\+  
Rule Engine  
\+  
Company Policy  
\+  
Risk Knowledge Base

Chứ không nên hỏi LLM đơn thuần:

> Is this risky?

Architecture:

Clause  
   ↓  
Risk Rules  
   \+  
Knowledge Base  
   \+  
LLM Reasoning  
   ↓  
Risk Classification  
   ↓  
Severity  
   ↓  
Explanation  
   ↓  
Recommendation

Member này chịu:

Legal Risk  
Financial Risk  
Operational Risk  
Compliance Risk  
General Contract Risk  
---

# **7\. AI Member 3 — Risk Scoring & Context Intelligence**

Member này chịu trách nhiệm biến hàng chục finding thành:

> **Một risk profile có ý nghĩa.**

Ví dụ:

Clause 03 → 85  
Clause 07 → 42  
Clause 12 → 91  
Clause 15 → 73

AI 3 không average đơn giản.

Phải xem:

Severity  
Business impact  
Likelihood  
Clause importance  
Contract type  
Vendor criticality  
Data sensitivity  
Service criticality

Ví dụ:

"No incident notification period"

Nếu vendor chỉ cung cấp:

Office chairs

Cyber Risk có thể thấp.

Nhưng nếu vendor cung cấp:

Cloud payroll system

và xử lý:

Employee PII  
Financial data

risk phải tăng rất nhiều.

Đó gọi là:

> **Context-aware risk assessment.**

AI 3 sở hữu:

Risk Scoring Engine  
Context Engine  
Risk Aggregation  
Risk Explanation  
Contract Comparison  
---

# **8\. Bây giờ tới role quan trọng: ATTT**

Tôi sẽ đặt tên role:

# **Cybersecurity & Third-Party Risk Engineer**

Không phải:

> người làm security cho website.

ATTT sẽ sở hữu **một Risk Domain hoàn chỉnh của sản phẩm**:

CYBERSECURITY CONTRACT RISK ENGINE

Đây là component ngang hàng với AI engines.

---

# **9\. ATTT xây Cybersecurity Clause Taxonomy**

ATTT phải xác định:

> Một hợp đồng B2B an toàn về cybersecurity cần xem xét những gì?

Ví dụ taxonomy:

| Cybersecurity Domain | ContractGuard tìm gì |
| ----- | ----- |
| Access Control | Access restriction |
| Authentication | MFA |
| Encryption | At rest / in transit |
| Data Classification | Sensitive data treatment |
| Incident Response | Breach notification |
| Logging | Security logging |
| Monitoring | Security monitoring |
| Vulnerability Management | Patch/vulnerability obligations |
| Security Testing | Pentest/audit |
| Backup | Backup requirements |
| Disaster Recovery | Recovery requirements |
| Business Continuity | Continuity obligations |
| Data Retention | Retention duration |
| Data Deletion | Secure deletion |
| Subprocessors | Third-party access |
| Audit Rights | Customer security audit |
| Compliance | ISO/NIST/SOC etc. |
| Termination | Access/data removal |
| Security Liability | Responsibility for incidents |

NIST CSF 2.0 explicitly includes cybersecurity supply-chain risk management, and NIST's current C-SCRM guidance emphasizes defining supplier requirements and performing supplier due diligence. That makes this a defensible foundation for your cybersecurity contract-risk taxonomy. [NIST Computer Security Resource Center](https://csrc.nist.gov/pubs/sp/1305/final?utm_source=chatgpt.com)

---

# **10\. Ví dụ ATTT Engine**

Contract:

"The vendor shall implement  
appropriate security measures."

LLM thông thường có thể:

> Looks acceptable.

Nhưng Cyber Risk Engine phải thấy:

"appropriate security measures"

quá mơ hồ.

Output:

Finding:  
Security requirements are not measurable.

Missing:  
\- Encryption requirements  
\- MFA requirements  
\- Logging requirements  
\- Vulnerability remediation  
\- Incident response requirements

Cyber Risk:  
HIGH

ATTT phải định nghĩa logic này.

---

# **11\. Ví dụ thứ hai**

Contract:

Supplier shall notify Customer  
of a cybersecurity incident.

Problem:

WHEN?

Không có:

24 hours  
48 hours  
72 hours

System:

Cyber Finding \#CY-014

Missing:  
Incident notification timeframe

Severity:  
HIGH

Impact:  
Delayed detection and response.

Recommendation:  
Define explicit notification timeframe  
and communication/escalation process.

Đây là **security contract analysis**, rất phù hợp với ATTT.

---

# **12\. Ví dụ thứ ba: Data deletion**

Contract:

Supplier may retain customer data  
after termination.

Hệ thống:

Data Security Risk

No explicit deletion period  
No secure deletion procedure  
No deletion confirmation

ATTT:

Risk \= HIGH  
---

# **13\. Ví dụ thứ tư: Subcontractor**

Vendor may engage subcontractors  
at its discretion.

Hệ thống phải thấy:

Company  
  ↓  
Vendor A  
  ↓  
Vendor B  
  ↓  
Vendor C

Data có thể chạy qua nhiều bên.

Finding:

Uncontrolled Fourth-Party Risk

Risk:  
HIGH

Đây là hướng **Cybersecurity Supply Chain Risk Management**, rất phù hợp với NIST SP 800-161. NIST mô tả C-SCRM là việc xác định, đánh giá và giảm cybersecurity risks xuyên suốt supply chain, không chỉ ở vendor trực tiếp. [NIST Computer Security Resource Center](https://csrc.nist.gov/pubs/sp/800/161/r1/upd1/final?utm_source=chatgpt.com)

---

# **14\. ATTT còn có nhiệm vụ thứ hai: Secure AI Pipeline**

Đây là phần rất đáng làm.

Contract là:

UNTRUSTED INPUT

Người dùng có thể upload:

PDF  
DOCX

chứa:

Malware  
Malicious links  
Prompt Injection  
Sensitive data  
Embedded objects

Vì vậy:

UPLOAD  
   ↓  
File Type Validation  
   ↓  
Malware Scan  
   ↓  
Document Sanitization  
   ↓  
Content Extraction  
   ↓  
Prompt Injection Detection  
   ↓  
LLM

ATTT sẽ chịu pipeline này.

---

# **15\. ATTT còn chịu Threat Modeling**

Threat actors:

Malicious User

Compromised Account

Malicious Vendor

External Attacker

Malicious Document

Insider

Attack surfaces:

Upload  
API  
Authentication  
LLM  
Vector Database  
Contract Database  
Export Report  
Admin

Sau đó Threat Model:

Threat  
 ↓  
Attack Vector  
 ↓  
Asset  
 ↓  
Impact  
 ↓  
Security Control

Đây là một deliverable ATTT rất đẹp.

---

# **16\. ATTT còn chịu AI Security**

Nếu dùng LLM \+ RAG:

Contract  
 ↓  
RAG  
 ↓  
LLM

sẽ có risks:

Prompt Injection  
Sensitive Information Disclosure  
Cross-tenant data leakage  
Unauthorized retrieval  
RAG poisoning  
Excessive AI permissions

Do đó phải thiết kế:

Company A Contract  
        ↓  
Tenant A Vector Store

Company B Contract  
        ↓  
Tenant B Vector Store

Không được:

Company A  
   ↓  
search  
   ↓  
Company B contracts

Đây là **multi-tenant security**, rất đáng để role ATTT làm.

---

# **17\. Role Dev**

Dev sở hữu:

Platform

Architecture:

Frontend  
   ↓  
Backend API  
   ↓  
Document Service  
   ↓  
AI Services  
   ↓  
Risk Engine  
   ↓  
Database

Dev làm:

| Component | Dev |
| ----- | ----- |
| Authentication UI | ✓ |
| Contract upload | ✓ |
| Contract viewer | ✓ |
| Risk dashboard | ✓ |
| Backend APIs | ✓ |
| Database | ✓ |
| Workflow | ✓ |
| Report generation | ✓ |
| AI integration | ✓ |
| Version control | ✓ |

ATTT thiết kế security requirement.

Dev implement cùng ATTT.

---

# **18\. Phân chia role cuối cùng**

| Role | Ownership |  |
| ----- | ----- | ----- |
| **AI 1** | Document & Clause Intelligence |  |
| **AI 2** | Contract Risk Analysis |  |
| **AI 3** | Context \+ Risk Scoring \+ Explanation |  |
| **Dev** | Platform & Integration |  |
| **ATTT** | Cyber Contract Risk \+ Platform Security \+ AI Security |  |

---

# **19\. Risk Engine cuối cùng**

Tôi đề xuất:

Contract Risk Engine  
│  
├── Legal Risk  
│  
├── Financial Risk  
│  
├── Operational Risk  
│  
├── Compliance Risk  
│  
├── Cybersecurity Risk    ← ATTT  
├── Privacy/Data Risk     ← AI \+ ATTT  
└── Third-Party Risk      ← AI \+ ATTT

Sau đó:

       CLAUSE RISKS  
              ↓  
      Context Adjustment  
              ↓  
        Risk Weighting  
              ↓  
      Contract Risk Score  
---

# **20\. Dashboard nên hiển thị thế này**

ABC CLOUD SERVICE AGREEMENT

Overall Risk  
━━━━━━━━━━━━━━━━  
74 / 100 HIGH

Risk Breakdown

Legal               58  
Financial           42  
Operational         61  
Compliance          72  
Cybersecurity       91  
Privacy             84  
Third Party         78

Bấm Cybersecurity:

CYBERSECURITY

5 Critical  
4 High  
3 Medium  
1 Low

Finding:

CY-001  
No breach notification deadline

CY-002  
No encryption requirement

CY-003  
No vulnerability remediation SLA

CY-004  
No subcontractor security requirements

CY-005  
No security audit rights

Đây là phần ATTT showcase rất rõ.

---

# **21\. User phải click được từng clause**

Ví dụ document viewer:

\-------------------------------  
17\. SECURITY  
\-------------------------------

The Supplier shall maintain  
appropriate security measures.

             🔴 HIGH RISK

Click:

Why is this risky?

→ "Appropriate security measures"  
   is undefined.

Missing controls:

✓ Encryption specification  
✓ Authentication requirements  
✓ Vulnerability management  
✓ Logging requirements  
✓ Incident response

Sau đó:

Recommended revision

AI đưa recommendation.

---

# **22\. Compare mode cực kỳ đáng làm**

User upload:

Contract Version 1  
Contract Version 2

AI:

V1  
Incident notification:  
24 hours

V2  
Incident notification:  
"promptly"

System:

Cybersecurity protection weakened

Risk:  
\+18

Hoặc:

V1 Liability cap:  
\$1M

V2 Liability cap:  
Unlimited

→

Legal/Financial Risk increased significantly.

Feature này có giá trị thực tế rất cao.

---

# **23\. Company Policy Engine**

Đây là feature tôi đặc biệt khuyên làm.

Company upload:

Contract Policy

Ví dụ:

All critical vendors must:

\- notify security incidents ≤24h  
\- encrypt sensitive data  
\- implement MFA  
\- retain logs ≥180 days  
\- delete company data after termination

ContractGuard:

Company Policy  
       vs  
Vendor Contract

→

5 requirements

Passed     2  
Partial    1  
Failed     2

Đây là cách project trở nên **enterprise-ready**.

---

# **24\. ATTT có thể map policy vào framework**

Ví dụ:

Company Requirement  
        ↓  
Security Control  
        ↓  
NIST / internal policy

NIST CSF 2.0 có hẳn hướng dẫn cho việc dùng CSF để **define and communicate supplier requirements**, rất sát use case của ContractGuard. [NIST Computer Security Resource Center](https://csrc.nist.gov/pubs/sp/1305/final?utm_source=chatgpt.com)

Tương lai có thể support:

NIST CSF  
ISO 27001  
SOC 2 requirements  
PCI DSS  
Internal Security Policy

Không cần support tất cả ở MVP.

---

# **25\. MVP tôi sẽ chốt**

### **Contract types**

Vendor Agreement  
Service Agreement  
Purchase Agreement  
MSA  
SOW

### **AI**

15–20 clause types

### **Risk domains**

Legal  
Financial  
Operational  
Compliance  
Cybersecurity  
Privacy  
Third Party

### **Cybersecurity**

Tối thiểu:

15–20 Cyber Risk Checks

### **Output**

Clause Risk  
Risk Category  
Severity  
Explanation  
Impact  
Recommendation  
Overall Risk Score  
---

# **26\. Roadmap rõ ràng cho team**

| Phase | Công việc | Lead |
| ----- | ----- | ----- |
| **P0** | Contract taxonomy \+ Risk taxonomy | AI2 \+ ATTT |
| **P1** | Platform architecture | Dev \+ ATTT |
| **P2** | Secure upload pipeline | Dev \+ **ATTT** |
| **P3** | Clause extraction | AI1 |
| **P4** | Entity extraction | AI1 |
| **P5** | General contract risk engine | AI2 |
| **P6** | Cyber contract risk engine | **ATTT \+ AI2** |
| **P7** | Context-aware scoring | AI3 |
| **P8** | Company policy comparison | AI3 |
| **P9** | AI explanation \+ recommendation | AI2 \+ AI3 |
| **P10** | Dashboard \+ document viewer | Dev |
| **P11** | Secure RAG \+ AI security | **ATTT** |
| **P12** | Pentest \+ model security testing | **ATTT** |
| **P13** | Evaluation | Cả team |

---

# **27\. Final demo nên là một B2B Cloud Service Contract**

Tôi đặc biệt khuyên demo bằng:

> **Company A ký hợp đồng sử dụng cloud/software service của Vendor B.**

Vì contract này chứa đủ:

Financial  
Legal  
Operational  
SLA  
Privacy  
Cybersecurity  
Third Party

Upload:

VendorCloud\_ServiceAgreement.pdf

AI trả:

24 clauses detected

Risk Engine:

8 Low  
6 Medium  
7 High  
3 Critical

Critical:

Unlimited Liability

No Security Incident  
Notification Deadline

Vendor May Retain Customer  
Data After Termination

Cybersecurity:

Encryption                 PASS  
MFA                        MISSING  
Incident Notification      HIGH  
Security Audit Rights      MISSING  
Subprocessors               HIGH  
Data Deletion               HIGH  
Logging                     PARTIAL  
Vulnerability SLA           MISSING

Overall:

CONTRACT RISK

79 / 100  
HIGH

Recommendation:  
Do not approve without review  
of 8 high/critical findings.

Đây là demo rất mạnh vì giảng viên nhìn vào sẽ hiểu ngay sản phẩm giải quyết vấn đề gì.

---

# **28\. Future vision**

Sau MVP, ContractGuard có thể phát triển:

Contract Analysis  
       ↓  
Contract Comparison  
       ↓  
Vendor Risk  
       ↓  
Cybersecurity Due Diligence  
       ↓  
Continuous Vendor Monitoring  
       ↓  
Contract Compliance Monitoring  
       ↓  
AI Negotiation Assistant  
       ↓  
Enterprise Third-Party Risk Platform

NIST cũng đang nhấn mạnh supplier due diligence như một phần quan trọng của cybersecurity supply-chain risk management; hướng phát triển từ **contract analysis → vendor due diligence → third-party cyber risk management** vì vậy rất hợp lý. [NIST](https://www.nist.gov/news-events/news/2026/07/nist-releases-finalized-c-scrm-due-diligence-assessment-quick-start-guide?utm_source=chatgpt.com)

### **Tôi sẽ chốt project của nhóm bằng một câu**

> **ContractGuard is an AI-powered B2B contract risk intelligence platform that analyzes every contractual clause across legal, financial, operational, compliance, privacy, third-party and cybersecurity dimensions, explains identified risks, recommends mitigations, and produces a contextual risk profile to support enterprise contract review and vendor risk management.**


Tính năng:

Quản lý:
- Scan hợp đồng
- Đa ngôn ngữ (Tiếng anh, việt)
- Upload, import hợp đồng (Văn bản)
- Phân quyền truy cập (tự cấp quyền giống như google workspace: cấp quyền qua tài khoảng email)

AI:
- Đánh giá rủi ro
- Đề xuất cải thiện
- Phân loại từng mục
