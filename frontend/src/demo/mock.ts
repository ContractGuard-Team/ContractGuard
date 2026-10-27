// shortcut: toàn bộ dữ liệu giả cho bản demo, xóa khi FE gọi API thật (xem frontend/DEMO-CLEANUP.md).
// Tên công ty, người và số hợp đồng đều hư cấu.
import type {
  AnalysisResult,
  Check,
  Contract,
  ContractDetail,
  Domain,
  Finding,
  Invite,
  Member,
  Severity,
  Share,
} from '../model'

export const ORG_NAME = 'Công ty CP Thương mại Lam Sơn Việt'
export const ORG_SHORT = 'Lam Sơn Việt'
export const CURRENT_USER_ID = 'm1'

export const MEMBERS: Member[] = [
  { id: 'm1', name: 'Trần Quốc Bảo', email: 'bao.tran@lamsonviet.vn', role: 'owner', title: 'Giám đốc', joined: '2026-08-02' },
  { id: 'm2', name: 'Lê Thu Hà', email: 'ha.le@lamsonviet.vn', role: 'member', title: 'Trưởng phòng mua hàng', joined: '2026-08-04' },
  { id: 'm3', name: 'Phạm Đức Anh', email: 'anh.pham@lamsonviet.vn', role: 'member', title: 'Phụ trách IT', joined: '2026-08-11' },
  { id: 'm4', name: 'Võ Ngọc Mai', email: 'mai.vo@lamsonviet.vn', role: 'member', title: 'Kế toán trưởng', joined: '2026-09-01' },
  { id: 'm5', name: 'Đỗ Hải Yến', email: 'yen.do@lamsonviet.vn', role: 'member', title: 'Kinh doanh', joined: '2026-09-15' },
]

export const INVITES: Invite[] = [{ email: 'lam.nguyen@lamsonviet.vn', role: 'member', sentAt: '2026-10-22T09:12:00' }]

const countOf = (findings: Finding[]) =>
  findings.reduce<Record<Severity, number>>(
    (acc, f) => ({ ...acc, [f.severity]: acc[f.severity] + 1 }),
    { critical: 0, high: 0, medium: 0, low: 0 },
  )

const scores = (s: [number, number, number, number, number | null, number | null, number | null]) => {
  const ids: Domain[] = ['legal', 'financial', 'operational', 'compliance', 'cyber', 'privacy', 'thirdparty']
  return Object.fromEntries(ids.map((id, i) => [id, s[i]])) as Record<Domain, number | null>
}

/* ------------------------------------------------------------------ */
/* Hợp đồng mẫu 1: dịch vụ điện toán đám mây (đủ 7 nhóm rủi ro)        */
/* ------------------------------------------------------------------ */

const cloudFindings: Finding[] = [
  {
    id: 'F-01', severity: 'critical', domain: 'legal', secondary: ['financial'], kind: 'issue', clauseId: '9.1',
    title: 'Trách nhiệm bồi thường của bên mình không có giới hạn',
    quote: 'Bên A chịu trách nhiệm bồi thường toàn bộ thiệt hại phát sinh từ hoặc liên quan đến việc Bên A vi phạm Hợp đồng này, không giới hạn về giá trị',
    why: 'Điều 9.1 buộc Bên A bồi thường toàn bộ thiệt hại mà không có mức trần, trong khi trách nhiệm của Bên B chỉ tối đa 01 tháng phí (Điều 9.2).',
    impact: 'Một vi phạm nhỏ, ví dụ chậm cung cấp dữ liệu đầu vào, có thể bị yêu cầu bồi thường vượt xa giá trị hợp đồng 486 triệu đồng.',
    recommendation: 'Đề nghị một mức trần trách nhiệm chung cho cả hai bên, thường bằng tổng phí 12 tháng, và loại trừ thiệt hại gián tiếp.',
  },
  {
    id: 'F-02', severity: 'critical', domain: 'cyber', secondary: ['compliance'], kind: 'issue', clauseId: '14',
    title: 'Không có thời hạn thông báo sự cố an ninh',
    quote: 'Bên B sẽ thông báo cho Bên A một cách kịp thời',
    why: 'Điều 14 chỉ ghi Bên B thông báo “một cách kịp thời”, không có số giờ cụ thể. Trong khi đó Dịch vụ xử lý thông tin cá nhân của 350 người lao động.',
    impact: 'Bên mình có thể biết chuyện lộ dữ liệu lương, căn cước, tài khoản ngân hàng của nhân viên sau nhiều ngày, mất cơ hội ứng phó và chậm thực hiện nghĩa vụ thông báo theo pháp luật về bảo vệ dữ liệu cá nhân.',
    recommendation: 'Yêu cầu thời hạn thông báo cụ thể, ví dụ trong vòng 24 giờ kể từ khi phát hiện, kèm nội dung tối thiểu của thông báo và đầu mối liên lạc khẩn cấp.',
    reference: 'ISO/IEC 27001:2022, A.5.24–A.5.26 (quản lý sự cố an toàn thông tin)',
  },
  {
    id: 'F-03', severity: 'critical', domain: 'privacy', kind: 'issue', clauseId: '18',
    title: 'Đối tác được giữ dữ liệu sau khi chấm dứt hợp đồng',
    quote: 'Sau khi Hợp đồng chấm dứt, Bên B có thể tiếp tục lưu giữ Dữ liệu của Bên A trong thời gian cần thiết theo chính sách nội bộ của Bên B',
    why: 'Điều 18 cho phép Bên B tiếp tục lưu dữ liệu “trong thời gian cần thiết theo chính sách nội bộ”, tức là không có thời hạn, không có nghĩa vụ xóa hay trả lại.',
    impact: 'Hồ sơ nhân sự và bảng lương của bên mình có thể nằm lại ở hệ thống đối tác vô thời hạn, kể cả khi hai bên đã chấm dứt quan hệ.',
    recommendation: 'Yêu cầu Bên B trả lại dữ liệu theo định dạng phổ biến và xóa toàn bộ bản sao trong thời hạn cụ thể (ví dụ 30 ngày), kèm biên bản xác nhận đã xóa.',
    reference: 'Luật Bảo vệ dữ liệu cá nhân 2025',
  },
  {
    id: 'F-04', severity: 'high', domain: 'financial', kind: 'issue', clauseId: '4.1',
    title: 'Trả trước 80% giá trị năm đầu',
    quote: 'Bên A thanh toán trước 80% phí dịch vụ năm trong vòng 05 ngày làm việc kể từ ngày ký Hợp đồng',
    why: 'Bên mình phải trả khoảng 388,8 triệu đồng trong 05 ngày làm việc sau khi ký, trước khi Dịch vụ được bàn giao và nghiệm thu.',
    impact: 'Nếu Bên B chậm bàn giao hoặc Dịch vụ không đạt yêu cầu, phần lớn tiền đã nằm ở phía đối tác và khó đòi lại.',
    recommendation: 'Đề nghị chia nhỏ đợt thanh toán, ví dụ 30% khi ký, 50% sau nghiệm thu, 20% sau 03 tháng vận hành ổn định.',
  },
  {
    id: 'F-05', severity: 'high', domain: 'financial', kind: 'issue', clauseId: '3',
    title: 'Đối tác được tăng phí không giới hạn khi gia hạn',
    quote: 'Từ năm thứ hai, Bên B được quyền điều chỉnh phí dịch vụ theo bảng giá niêm yết tại thời điểm gia hạn',
    why: 'Từ năm thứ hai, phí áp theo “bảng giá niêm yết tại thời điểm gia hạn” do Bên B tự quyết, không có mức trần.',
    impact: 'Kết hợp với điều khoản tự động gia hạn (Điều 6), bên mình có thể phải trả mức phí mới cao hơn nhiều mà không kịp phản ứng.',
    recommendation: 'Đề nghị giới hạn mức tăng (ví dụ không quá 5–10% mỗi năm) và yêu cầu Bên B báo giá mới trước ít nhất 120 ngày.',
  },
  {
    id: 'F-06', severity: 'high', domain: 'financial', secondary: ['operational'], kind: 'issue', clauseId: '5',
    title: 'Lãi chậm trả 0,5%/ngày và bị ngừng dịch vụ không báo trước',
    quote: 'Bên A phải trả lãi chậm trả bằng 0,5%/ngày trên số tiền chậm trả',
    why: 'Lãi 0,5%/ngày tương đương khoảng 182%/năm. Ngoài ra Bên B được tạm ngừng Dịch vụ ngay mà không cần thông báo.',
    impact: 'Chỉ một lần chậm thanh toán vài ngày cũng có thể làm hệ thống tính lương ngừng giữa kỳ trả lương.',
    recommendation: 'Đề nghị lãi chậm trả theo mức lãi suất thị trường, và chỉ được tạm ngừng sau khi đã thông báo bằng văn bản và cho thời gian khắc phục (ví dụ 15 ngày).',
    reference: 'Luật Thương mại 2005, Điều 306 (tiền lãi do chậm thanh toán)',
  },
  {
    id: 'F-07', severity: 'high', domain: 'legal', secondary: ['financial'], kind: 'issue', clauseId: '9.2',
    title: 'Trách nhiệm của đối tác tối đa chỉ bằng 01 tháng phí',
    quote: 'Tổng trách nhiệm của Bên B trong mọi trường hợp không vượt quá phí dịch vụ của 01 tháng liền trước thời điểm phát sinh thiệt hại',
    why: 'Dù thiệt hại lớn đến đâu, Bên B chỉ bồi thường tối đa khoảng 40,5 triệu đồng, bằng 1/12 phí năm.',
    impact: 'Nếu sự cố của Bên B làm sai bảng lương hoặc lộ dữ liệu, phần lớn thiệt hại bên mình phải tự chịu.',
    recommendation: 'Đề nghị trần trách nhiệm ít nhất bằng tổng phí 12 tháng, và không áp dụng trần với vi phạm bảo mật dữ liệu.',
  },
  {
    id: 'F-08', severity: 'high', domain: 'legal', kind: 'issue', clauseId: '17.2',
    title: 'Quyền chấm dứt hợp đồng không cân bằng',
    quote: 'Bên A chỉ được đơn phương chấm dứt Hợp đồng khi Bên B vi phạm nghiêm trọng nghĩa vụ và không khắc phục trong vòng 60 ngày kể từ ngày nhận được thông báo',
    why: 'Bên B được chấm dứt chỉ với thông báo trước 30 ngày (Điều 17.1). Bên mình chỉ được chấm dứt khi Bên B vi phạm nghiêm trọng và để quá 60 ngày, đồng thời mất toàn bộ phí đã trả.',
    impact: 'Bên mình có thể bị kẹt với một Dịch vụ kém chất lượng cả năm, trong khi đối tác rút lui được bất cứ lúc nào.',
    recommendation: 'Đề nghị quyền chấm dứt ngang nhau, rút thời gian khắc phục xuống 15–30 ngày và hoàn lại phí tương ứng với thời gian chưa sử dụng.',
  },
  {
    id: 'F-09', severity: 'high', domain: 'operational', kind: 'issue', clauseId: '8',
    title: 'Cam kết uptime 99% nhưng không có đền bù',
    quote: 'Trường hợp không đạt cam kết, Bên B sẽ nỗ lực khắc phục trong thời gian sớm nhất',
    why: 'Mức 99% mỗi tháng cho phép Dịch vụ ngừng khoảng 7,3 giờ/tháng. Khi không đạt, Bên B chỉ “nỗ lực khắc phục”, không có khoản giảm trừ phí nào.',
    impact: 'Hệ thống có thể ngừng đúng ngày chốt lương mà bên mình không có căn cứ nào để yêu cầu bồi hoàn.',
    recommendation: 'Đề nghị nâng cam kết lên 99,5–99,9% và bổ sung bảng giảm trừ phí (service credit) theo từng mức không đạt.',
  },
  {
    id: 'F-10', severity: 'medium', domain: 'operational', kind: 'issue', clauseId: '7',
    title: 'Nghiệm thu mặc nhiên sau 03 ngày làm việc',
    quote: 'Dịch vụ được coi là đã được Bên A nghiệm thu nếu trong vòng 03 ngày làm việc kể từ ngày bàn giao, Bên A không có ý kiến bằng văn bản',
    why: 'Nếu bên mình không phản hồi bằng văn bản trong 03 ngày làm việc, Dịch vụ coi như đã được nghiệm thu. Hợp đồng cũng không có tiêu chí nghiệm thu.',
    impact: 'Ba ngày là quá ngắn để kiểm tra một hệ thống tính lương. Sau nghiệm thu, 20% còn lại đến hạn thanh toán.',
    recommendation: 'Đề nghị tiêu chí nghiệm thu cụ thể (ví dụ chạy thử trọn một kỳ lương) và thời hạn kiểm tra tối thiểu 15 ngày làm việc.',
  },
  {
    id: 'F-11', severity: 'high', domain: 'privacy', kind: 'issue', clauseId: '12.2',
    title: 'Đối tác được dùng dữ liệu của mình để phát triển sản phẩm',
    quote: 'Bên B được phép sử dụng Dữ liệu của Bên A dưới dạng tổng hợp để phân tích, cải thiện và phát triển sản phẩm, dịch vụ của Bên B',
    why: 'Điều 12.2 cho phép Bên B dùng dữ liệu “dưới dạng tổng hợp” cho mục đích của chính Bên B, nhưng không định nghĩa “tổng hợp” và không yêu cầu ẩn danh.',
    impact: 'Dữ liệu lương và nhân sự có thể bị dùng ngoài mục đích cung cấp Dịch vụ cho bên mình.',
    recommendation: 'Đề nghị giới hạn mục đích xử lý chỉ để cung cấp Dịch vụ. Nếu chấp nhận dữ liệu tổng hợp, yêu cầu ẩn danh hoàn toàn và không thể truy ngược.',
    reference: 'Luật Bảo vệ dữ liệu cá nhân 2025',
  },
  {
    id: 'F-12', severity: 'medium', domain: 'privacy', secondary: ['compliance'], kind: 'issue', clauseId: '12.3',
    title: 'Dữ liệu có thể được chuyển ra nước ngoài',
    quote: 'có thể được lưu trữ và xử lý tại các trung tâm dữ liệu của Bên B hoặc của đối tác của Bên B, ở trong hoặc ngoài lãnh thổ Việt Nam',
    why: 'Dữ liệu có thể nằm ở trung tâm dữ liệu của đối tác của Bên B, kể cả ngoài Việt Nam, mà không cần báo trước hay được bên mình đồng ý.',
    impact: 'Việc chuyển dữ liệu cá nhân ra nước ngoài có thể kéo theo nghĩa vụ pháp lý cho chính bên mình.',
    recommendation: 'Đề nghị Bên B nêu rõ vị trí lưu trữ, thông báo trước khi thay đổi và hỗ trợ hồ sơ pháp lý cần thiết.',
    reference: 'Luật Bảo vệ dữ liệu cá nhân 2025',
  },
  {
    id: 'F-13', severity: 'high', domain: 'cyber', kind: 'issue', clauseId: '13',
    title: 'Cam kết bảo mật chung chung, không đo lường được',
    quote: 'Bên B áp dụng các biện pháp bảo mật phù hợp để bảo vệ Dịch vụ và Dữ liệu của Bên A',
    why: '“Biện pháp bảo mật phù hợp” không nói rõ là biện pháp gì. Ngoài mã hóa, hợp đồng không nhắc tới kiểm soát truy cập, ghi log, vá lỗ hổng, kiểm thử bảo mật hay chứng chỉ.',
    impact: 'Khi có sự cố, rất khó chứng minh Bên B đã vi phạm nghĩa vụ bảo mật.',
    recommendation: 'Đề nghị một phụ lục yêu cầu bảo mật cụ thể: phân quyền tối thiểu, lưu log ít nhất 12 tháng, vá lỗ hổng nghiêm trọng trong 7 ngày, kiểm thử xâm nhập hằng năm, duy trì chứng nhận ISO/IEC 27001.',
    reference: 'ISO/IEC 27001:2022, A.5.20 (an toàn thông tin trong thỏa thuận với nhà cung cấp)',
  },
  {
    id: 'F-14', severity: 'high', domain: 'thirdparty', kind: 'issue', clauseId: '16',
    title: 'Đối tác được thuê nhà thầu phụ mà không cần mình đồng ý',
    quote: 'Bên B có quyền sử dụng nhà thầu phụ hoặc bên xử lý dữ liệu khác để thực hiện một phần Dịch vụ mà không cần sự chấp thuận của Bên A',
    why: 'Bên B được giao một phần Dịch vụ cho bên khác mà không cần chấp thuận, và còn loại trừ trách nhiệm với sai sót của nhà thầu phụ.',
    impact: 'Dữ liệu nhân sự có thể đi qua bên thứ ba, thứ tư mà bên mình không biết và không thể yêu cầu ai chịu trách nhiệm.',
    recommendation: 'Đề nghị Bên B công bố danh sách bên xử lý phụ, thông báo trước khi thay đổi, áp nghĩa vụ bảo mật tương đương cho nhà thầu phụ và chịu trách nhiệm liên đới.',
    reference: 'ISO/IEC 27001:2022, A.5.19–A.5.21 (quan hệ với nhà cung cấp)',
  },
  {
    id: 'F-15', severity: 'medium', domain: 'thirdparty', secondary: ['cyber'], kind: 'missing',
    title: 'Không có quyền kiểm toán bảo mật đối tác',
    missingWhat: 'quyền kiểm toán bảo mật của Bên A',
    why: 'Không điều khoản nào cho phép bên mình hoặc một đơn vị độc lập kiểm tra việc Bên B thực hiện nghĩa vụ bảo mật, hay yêu cầu báo cáo đánh giá định kỳ.',
    impact: 'Bên mình không có cách nào xác minh các cam kết bảo mật ngoài lời nói của đối tác.',
    recommendation: 'Đề nghị quyền yêu cầu báo cáo đánh giá độc lập hằng năm (ví dụ chứng nhận ISO/IEC 27001) và quyền kiểm toán khi xảy ra sự cố.',
  },
  {
    id: 'F-16', severity: 'medium', domain: 'cyber', kind: 'missing',
    title: 'Không yêu cầu xác thực đa yếu tố (MFA)',
    missingWhat: 'xác thực đa yếu tố',
    why: 'Hợp đồng không yêu cầu MFA cho tài khoản quản trị của Bên B, cũng không cho phép bên mình bật MFA cho người dùng của mình.',
    impact: 'Chỉ cần lộ một mật khẩu quản trị là có thể truy cập toàn bộ dữ liệu lương.',
    recommendation: 'Đề nghị bắt buộc MFA cho mọi truy cập quản trị và cho phép bên mình bật MFA cho người dùng của mình.',
  },
  {
    id: 'F-17', severity: 'medium', domain: 'cyber', kind: 'issue', clauseId: '15',
    title: 'Sao lưu không rõ tần suất và thời gian khôi phục',
    quote: 'Bên B thực hiện sao lưu Dữ liệu của Bên A định kỳ theo quy trình vận hành của Bên B',
    why: '“Định kỳ theo quy trình của Bên B” không cho biết bao lâu sao lưu một lần, giữ bản sao bao lâu và mất bao lâu để khôi phục.',
    impact: 'Khi có sự cố, bên mình có thể mất dữ liệu nhiều ngày mà không có căn cứ để yêu cầu.',
    recommendation: 'Đề nghị sao lưu tối thiểu hằng ngày, cam kết thời gian khôi phục (RTO) và lượng dữ liệu tối đa có thể mất (RPO).',
  },
  {
    id: 'F-18', severity: 'medium', domain: 'operational', kind: 'issue', clauseId: '2',
    title: 'Đối tác được tự ý ngừng một số tính năng',
    quote: 'Bên B có quyền điều chỉnh, bổ sung hoặc ngừng cung cấp một số tính năng của Dịch vụ theo chính sách sản phẩm từng thời kỳ',
    why: 'Bên B được ngừng tính năng “theo chính sách sản phẩm từng thời kỳ”, không cần báo trước và không giảm phí.',
    impact: 'Mô-đun bên mình đang dùng có thể bị cắt giữa kỳ hợp đồng.',
    recommendation: 'Đề nghị cam kết duy trì các mô-đun tại Phụ lục 01 trong suốt thời hạn hợp đồng, hoặc giảm phí và báo trước ít nhất 90 ngày.',
  },
  {
    id: 'F-19', severity: 'medium', domain: 'financial', kind: 'issue', clauseId: '6',
    title: 'Tự động gia hạn, phải báo trước 90 ngày nếu muốn dừng',
    quote: 'tự động gia hạn từng kỳ 12 tháng, trừ khi một Bên thông báo không gia hạn bằng văn bản ít nhất 90 ngày trước ngày hết hạn',
    why: 'Nếu quên gửi thông báo trước 90 ngày, hợp đồng tự kéo dài thêm 12 tháng với mức phí mới do Bên B đặt (Điều 3).',
    impact: 'Bên mình dễ bị gia hạn ngoài ý muốn và chịu mức giá do đối tác tự quyết.',
    recommendation: 'Đề nghị rút thời hạn báo trước xuống 30 ngày và yêu cầu Bên B nhắc trước ngày gia hạn.',
  },
  {
    id: 'F-20', severity: 'medium', domain: 'legal', secondary: ['cyber'], kind: 'issue', clauseId: '19',
    title: 'Tấn công mạng được coi là bất khả kháng',
    quote: 'sự cố của nhà cung cấp hạ tầng và các cuộc tấn công mạng',
    why: 'Điều 19 xếp “sự cố của nhà cung cấp hạ tầng” và “các cuộc tấn công mạng” vào bất khả kháng, nên Bên B được miễn trách nhiệm.',
    impact: 'Đúng loại sự cố mà một nhà cung cấp cloud phải phòng ngừa lại trở thành lý do để đối tác không chịu trách nhiệm.',
    recommendation: 'Đề nghị loại tấn công mạng và sự cố hạ tầng khỏi danh sách bất khả kháng, trừ khi Bên B chứng minh đã áp dụng đủ các biện pháp bảo mật đã cam kết.',
  },
  {
    id: 'F-21', severity: 'medium', domain: 'legal', kind: 'issue', clauseId: '10',
    title: 'Dịch vụ được cung cấp “theo hiện trạng”',
    quote: 'Dịch vụ được cung cấp theo hiện trạng',
    why: 'Bên B không bảo đảm Dịch vụ hoạt động liên tục, không có lỗi hay đáp ứng yêu cầu nghiệp vụ của bên mình.',
    impact: 'Lỗi tính lương do phần mềm có thể không bị coi là vi phạm hợp đồng.',
    recommendation: 'Đề nghị Bên B bảo đảm Dịch vụ hoạt động đúng mô tả tại Phụ lục 01 và sửa lỗi trong thời hạn cụ thể.',
  },
  {
    id: 'F-22', severity: 'low', domain: 'legal', kind: 'issue', clauseId: '11',
    title: 'Thời hạn bảo mật thông tin 02 năm sau chấm dứt',
    quote: '02 năm sau khi Hợp đồng chấm dứt',
    why: 'Thời hạn này ngắn hơn mức 3–5 năm thường gặp, nhưng chấp nhận được với thông tin kinh doanh thông thường.',
    impact: 'Ảnh hưởng thấp.',
    recommendation: 'Có thể đề nghị 03 năm nếu bên mình chia sẻ thông tin chiến lược với đối tác.',
  },
  {
    id: 'F-23', severity: 'low', domain: 'legal', kind: 'issue', clauseId: '20',
    title: 'Giải quyết tranh chấp bằng trọng tài trong nước',
    quote: 'Trung tâm Trọng tài Quốc tế Việt Nam (VIAC)',
    why: 'Luật Việt Nam và trọng tài trong nước là lựa chọn phổ biến, thuận lợi cho bên mình.',
    impact: 'Chi phí trọng tài cao hơn tòa án nhưng thời gian giải quyết thường nhanh hơn.',
    recommendation: 'Không cần thay đổi.',
  },
]

const cloudChecks: Check[] = [
  { id: 'CY-01', name: 'Yêu cầu bảo mật đo lường được', status: 'partial', clauseId: '13', findingId: 'F-13' },
  { id: 'CY-02', name: 'Mã hóa dữ liệu khi truyền và khi lưu', status: 'pass', clauseId: '13' },
  { id: 'CY-03', name: 'Kiểm soát truy cập, phân quyền tối thiểu', status: 'missing', findingId: 'F-13', note: 'Gộp trong F-13' },
  { id: 'CY-04', name: 'Xác thực đa yếu tố (MFA)', status: 'missing', findingId: 'F-16' },
  { id: 'CY-05', name: 'Ghi log, giám sát và thời gian lưu log', status: 'missing', findingId: 'F-13', note: 'Gộp trong F-13' },
  { id: 'CY-06', name: 'Quản lý lỗ hổng, thời hạn vá lỗi', status: 'missing', findingId: 'F-13', note: 'Gộp trong F-13' },
  { id: 'CY-07', name: 'Kiểm thử bảo mật định kỳ', status: 'missing', findingId: 'F-13', note: 'Gộp trong F-13' },
  { id: 'CY-08', name: 'Thời hạn thông báo sự cố (24/48/72 giờ)', status: 'partial', clauseId: '14', findingId: 'F-02' },
  { id: 'CY-09', name: 'Quy trình ứng phó và đầu mối liên lạc sự cố', status: 'missing', findingId: 'F-02', note: 'Gộp trong F-02' },
  { id: 'CY-10', name: 'Sao lưu, khôi phục (RTO/RPO)', status: 'partial', clauseId: '15', findingId: 'F-17' },
  { id: 'CY-11', name: 'Chứng chỉ ISO/IEC 27001, SOC 2', status: 'missing', findingId: 'F-13', note: 'Gộp trong F-13' },
  { id: 'CY-12', name: 'Trách nhiệm khi xảy ra sự cố bảo mật', status: 'partial', clauseId: '19', findingId: 'F-20' },
  { id: 'PR-01', name: 'Quyền sở hữu dữ liệu thuộc bên mình', status: 'pass', clauseId: '12.1' },
  { id: 'PR-02', name: 'Giới hạn mục đích xử lý dữ liệu', status: 'partial', clauseId: '12.2', findingId: 'F-11' },
  { id: 'PR-03', name: 'Vị trí lưu trữ, chuyển dữ liệu ra nước ngoài', status: 'partial', clauseId: '12.3', findingId: 'F-12' },
  { id: 'PR-04', name: 'Thời hạn lưu trữ dữ liệu', status: 'missing', findingId: 'F-03', note: 'Gộp trong F-03' },
  { id: 'PR-05', name: 'Xóa hoặc trả dữ liệu khi chấm dứt, có xác nhận', status: 'missing', clauseId: '18', findingId: 'F-03' },
  { id: 'TP-01', name: 'Kiểm soát nhà thầu phụ (phê duyệt hoặc báo trước)', status: 'missing', clauseId: '16', findingId: 'F-14' },
  { id: 'TP-02', name: 'Nghĩa vụ bảo mật áp xuống nhà thầu phụ', status: 'missing', clauseId: '16', findingId: 'F-14', note: 'Gộp trong F-14' },
  { id: 'TP-03', name: 'Quyền kiểm toán bảo mật của bên mình', status: 'missing', findingId: 'F-15' },
]

const cloudDetail: ContractDetail = {
  heading: 'HỢP ĐỒNG CUNG CẤP DỊCH VỤ ĐIỆN TOÁN ĐÁM MÂY',
  number: 'Số: 0917/2026/HĐDV-MNV',
  preamble: 'Hôm nay, ngày 15 tháng 10 năm 2026, tại Thành phố Hồ Chí Minh, chúng tôi gồm:',
  partyA: { label: 'Bên A (Bên sử dụng dịch vụ)', name: ORG_NAME, rep: 'Ông Trần Quốc Bảo, Giám đốc' },
  partyB: { label: 'Bên B (Bên cung cấp dịch vụ)', name: 'Công ty TNHH Giải pháp Mây Nam Việt', rep: 'Bà Huỳnh Thị Kim Oanh, Tổng Giám đốc' },
  articles: [
    { number: '1', title: 'Định nghĩa', page: 1, clauses: [{ id: '1', text: 'Trong Hợp đồng này, “Dịch vụ” là phần mềm tính lương và quản lý nhân sự do Bên B cung cấp trên nền tảng điện toán đám mây; “Dữ liệu của Bên A” là mọi thông tin, tệp tin do Bên A hoặc người lao động của Bên A đưa lên Dịch vụ, bao gồm thông tin cá nhân của người lao động.' }] },
    { number: '2', title: 'Phạm vi dịch vụ', page: 1, clauses: [{ id: '2', text: 'Bên B cung cấp cho Bên A quyền sử dụng Dịch vụ cho tối đa 350 người lao động, gồm các mô-đun được liệt kê tại Phụ lục 01. Bên B có quyền điều chỉnh, bổ sung hoặc ngừng cung cấp một số tính năng của Dịch vụ theo chính sách sản phẩm từng thời kỳ.' }] },
    { number: '3', title: 'Phí dịch vụ', page: 1, clauses: [{ id: '3', text: 'Phí dịch vụ năm đầu tiên là 486.000.000 đồng (bằng chữ: Bốn trăm tám mươi sáu triệu đồng), chưa bao gồm thuế giá trị gia tăng. Từ năm thứ hai, Bên B được quyền điều chỉnh phí dịch vụ theo bảng giá niêm yết tại thời điểm gia hạn.' }] },
    { number: '4', title: 'Thanh toán', page: 2, clauses: [
      { id: '4.1', text: 'Bên A thanh toán trước 80% phí dịch vụ năm trong vòng 05 ngày làm việc kể từ ngày ký Hợp đồng; 20% còn lại được thanh toán sau khi nghiệm thu Dịch vụ.' },
      { id: '4.2', text: 'Bên B xuất hóa đơn giá trị gia tăng hợp lệ cho từng khoản thanh toán theo quy định của pháp luật về thuế.' },
    ] },
    { number: '5', title: 'Chậm thanh toán', page: 2, clauses: [{ id: '5', text: 'Trường hợp Bên A chậm thanh toán, Bên A phải trả lãi chậm trả bằng 0,5%/ngày trên số tiền chậm trả. Bên B có quyền tạm ngừng cung cấp Dịch vụ mà không cần thông báo trước cho đến khi Bên A thanh toán đầy đủ.' }] },
    { number: '6', title: 'Thời hạn và gia hạn', page: 2, clauses: [{ id: '6', text: 'Hợp đồng có hiệu lực 12 tháng kể từ ngày ký và tự động gia hạn từng kỳ 12 tháng, trừ khi một Bên thông báo không gia hạn bằng văn bản ít nhất 90 ngày trước ngày hết hạn.' }] },
    { number: '7', title: 'Bàn giao và nghiệm thu', page: 2, clauses: [{ id: '7', text: 'Bên B bàn giao tài khoản quản trị Dịch vụ cho Bên A trong vòng 10 ngày kể từ ngày nhận khoản thanh toán trước. Dịch vụ được coi là đã được Bên A nghiệm thu nếu trong vòng 03 ngày làm việc kể từ ngày bàn giao, Bên A không có ý kiến bằng văn bản.' }] },
    { number: '8', title: 'Chất lượng dịch vụ', page: 2, clauses: [{ id: '8', text: 'Bên B cam kết thời gian hoạt động của Dịch vụ đạt tối thiểu 99,0% mỗi tháng, không bao gồm thời gian bảo trì theo kế hoạch. Trường hợp không đạt cam kết, Bên B sẽ nỗ lực khắc phục trong thời gian sớm nhất.' }] },
    { number: '9', title: 'Giới hạn trách nhiệm', page: 3, clauses: [
      { id: '9.1', text: 'Bên A chịu trách nhiệm bồi thường toàn bộ thiệt hại phát sinh từ hoặc liên quan đến việc Bên A vi phạm Hợp đồng này, không giới hạn về giá trị.' },
      { id: '9.2', text: 'Tổng trách nhiệm của Bên B trong mọi trường hợp không vượt quá phí dịch vụ của 01 tháng liền trước thời điểm phát sinh thiệt hại.' },
    ] },
    { number: '10', title: 'Bảo hành', page: 3, clauses: [{ id: '10', text: 'Dịch vụ được cung cấp theo hiện trạng. Bên B không bảo đảm Dịch vụ hoạt động liên tục, không có lỗi hoặc đáp ứng mọi yêu cầu nghiệp vụ của Bên A.' }] },
    { number: '11', title: 'Bảo mật thông tin', page: 3, clauses: [{ id: '11', text: 'Mỗi Bên có nghĩa vụ giữ bí mật các thông tin kinh doanh của Bên kia mà mình biết được trong quá trình thực hiện Hợp đồng, trong thời hạn Hợp đồng và 02 năm sau khi Hợp đồng chấm dứt.' }] },
    { number: '12', title: 'Dữ liệu của Bên A', page: 3, clauses: [
      { id: '12.1', text: 'Dữ liệu của Bên A thuộc quyền sở hữu của Bên A.' },
      { id: '12.2', text: 'Bên B được phép sử dụng Dữ liệu của Bên A dưới dạng tổng hợp để phân tích, cải thiện và phát triển sản phẩm, dịch vụ của Bên B.' },
      { id: '12.3', text: 'Dữ liệu của Bên A có thể được lưu trữ và xử lý tại các trung tâm dữ liệu của Bên B hoặc của đối tác của Bên B, ở trong hoặc ngoài lãnh thổ Việt Nam.' },
    ] },
    { number: '13', title: 'An toàn thông tin', page: 3, clauses: [{ id: '13', text: 'Bên B áp dụng các biện pháp bảo mật phù hợp để bảo vệ Dịch vụ và Dữ liệu của Bên A. Dữ liệu của Bên A được mã hóa khi truyền tải và khi lưu trữ.' }] },
    { number: '14', title: 'Sự cố an ninh mạng', page: 4, clauses: [{ id: '14', text: 'Khi phát hiện sự cố an ninh ảnh hưởng đến Dữ liệu của Bên A, Bên B sẽ thông báo cho Bên A một cách kịp thời và phối hợp khắc phục hậu quả.' }] },
    { number: '15', title: 'Sao lưu dữ liệu', page: 4, clauses: [{ id: '15', text: 'Bên B thực hiện sao lưu Dữ liệu của Bên A định kỳ theo quy trình vận hành của Bên B.' }] },
    { number: '16', title: 'Nhà thầu phụ', page: 4, clauses: [{ id: '16', text: 'Bên B có quyền sử dụng nhà thầu phụ hoặc bên xử lý dữ liệu khác để thực hiện một phần Dịch vụ mà không cần sự chấp thuận của Bên A. Bên B không chịu trách nhiệm đối với các sai sót do nhà thầu phụ gây ra.' }] },
    { number: '17', title: 'Chấm dứt hợp đồng', page: 4, clauses: [
      { id: '17.1', text: 'Bên B có quyền đơn phương chấm dứt Hợp đồng bằng việc thông báo cho Bên A trước 30 ngày.' },
      { id: '17.2', text: 'Bên A chỉ được đơn phương chấm dứt Hợp đồng khi Bên B vi phạm nghiêm trọng nghĩa vụ và không khắc phục trong vòng 60 ngày kể từ ngày nhận được thông báo. Trong trường hợp này, Bên A không được hoàn lại phí dịch vụ đã thanh toán.' },
    ] },
    { number: '18', title: 'Dữ liệu sau khi chấm dứt', page: 5, clauses: [{ id: '18', text: 'Sau khi Hợp đồng chấm dứt, Bên B có thể tiếp tục lưu giữ Dữ liệu của Bên A trong thời gian cần thiết theo chính sách nội bộ của Bên B.' }] },
    { number: '19', title: 'Bất khả kháng', page: 5, clauses: [{ id: '19', text: 'Sự kiện bất khả kháng bao gồm nhưng không giới hạn ở thiên tai, dịch bệnh, chiến tranh, sự cố của nhà cung cấp hạ tầng và các cuộc tấn công mạng. Bên bị ảnh hưởng được miễn trách nhiệm đối với việc không thực hiện nghĩa vụ trong thời gian xảy ra sự kiện bất khả kháng.' }] },
    { number: '20', title: 'Giải quyết tranh chấp', page: 5, clauses: [{ id: '20', text: 'Hợp đồng được điều chỉnh bởi pháp luật Việt Nam. Mọi tranh chấp phát sinh được giải quyết tại Trung tâm Trọng tài Quốc tế Việt Nam (VIAC) theo Quy tắc tố tụng trọng tài của Trung tâm này.' }] },
    { number: '21', title: 'Điều khoản thi hành', page: 5, clauses: [{ id: '21', text: 'Hợp đồng được lập thành 04 bản có giá trị pháp lý như nhau, mỗi Bên giữ 02 bản, có hiệu lực kể từ ngày ký.' }] },
  ],
  findings: cloudFindings,
  checks: cloudChecks,
  chat: [
    {
      q: 'Rủi ro lớn nhất của hợp đồng này là gì?',
      keywords: ['lớn nhất', 'nghiêm trọng', 'tóm tắt', 'quan trọng nhất', 'rủi ro chính', 'nên lo'],
      a: 'Có 3 điểm nghiêm trọng nên xử lý trước khi ký:\n\n1. Bên mình phải bồi thường không giới hạn, trong khi đối tác chỉ chịu tối đa 01 tháng phí [[9.1]] [[9.2]].\n\n2. Đối tác chỉ hứa báo sự cố an ninh “kịp thời”, không có thời hạn cụ thể, dù hệ thống chứa dữ liệu cá nhân của 350 nhân viên [[14]].\n\n3. Sau khi chấm dứt, đối tác được giữ dữ liệu của mình theo chính sách nội bộ của họ, không có nghĩa vụ xóa [[18]].',
    },
    {
      q: 'Nếu bên B bị lộ dữ liệu thì họ phải báo cho mình trong bao lâu?',
      keywords: ['lộ', 'sự cố', 'báo cho', 'thông báo', 'rò rỉ', 'tấn công', 'hack', 'bao lâu'],
      a: 'Hợp đồng không quy định thời hạn cụ thể. Điều 14 chỉ ghi Bên B sẽ thông báo “một cách kịp thời” [[14]], nên đối tác báo sau vài ngày vẫn không vi phạm.\n\nThêm nữa, Điều 19 coi tấn công mạng là bất khả kháng [[19]], nên đối tác còn có thể được miễn trách nhiệm.\n\nBạn nên đề nghị thời hạn thông báo trong vòng 24 giờ kể từ khi phát hiện, kèm đầu mối liên lạc khẩn cấp.',
    },
    {
      q: 'Mình có thể chấm dứt hợp đồng sớm không?',
      keywords: ['chấm dứt', 'hủy', 'dừng', 'thoát', 'kết thúc', 'rút'],
      a: 'Rất khó. Bên mình chỉ được chấm dứt khi Bên B vi phạm nghiêm trọng và không khắc phục trong 60 ngày, và khi đó cũng không được hoàn lại phí đã trả [[17.2]].\n\nNgược lại, Bên B được chấm dứt bất cứ lúc nào, chỉ cần báo trước 30 ngày [[17.1]]. Nếu muốn dừng ở cuối kỳ, bên mình phải báo trước 90 ngày, nếu không hợp đồng tự gia hạn [[6]].',
    },
    {
      q: 'Phí năm sau có thể tăng bao nhiêu?',
      keywords: ['phí', 'giá', 'tăng', 'năm sau', 'gia hạn', 'chi phí', 'tiền'],
      a: 'Không có giới hạn. Từ năm thứ hai, phí áp theo bảng giá niêm yết do Bên B tự đặt tại thời điểm gia hạn [[3]]. Vì hợp đồng tự gia hạn nếu bên mình không báo trước 90 ngày [[6]], bên mình có thể bị áp giá mới mà không kịp phản ứng.\n\nNên đề nghị mức tăng tối đa (ví dụ 5–10% mỗi năm) và yêu cầu báo giá trước 120 ngày.',
    },
    {
      q: 'Dữ liệu nhân viên có bị đưa ra nước ngoài không?',
      keywords: ['nước ngoài', 'ngoài lãnh thổ', 'lưu ở đâu', 'lưu trữ ở', 'vị trí', 'chuyển dữ liệu', 'trung tâm dữ liệu'],
      a: 'Có thể. Hợp đồng cho phép dữ liệu được lưu và xử lý tại trung tâm dữ liệu của Bên B hoặc của đối tác của họ, ở trong hoặc ngoài Việt Nam [[12.3]]. Bên B còn được dùng nhà thầu phụ mà không cần bên mình đồng ý [[16]].\n\nBên mình nên yêu cầu nêu rõ vị trí lưu trữ và được thông báo trước mọi thay đổi.',
    },
    {
      q: 'Nếu hệ thống ngừng hoạt động thì có được đền bù không?',
      keywords: ['ngừng hoạt động', 'sập', 'uptime', 'sla', 'đền bù', 'gián đoạn', 'downtime', 'không hoạt động'],
      a: 'Không. Bên B cam kết uptime 99% mỗi tháng, tức khoảng 7,3 giờ được phép ngừng, nhưng khi không đạt chỉ “nỗ lực khắc phục” [[8]]. Hợp đồng không có khoản giảm trừ phí nào.\n\nĐiều 10 còn ghi Dịch vụ được cung cấp theo hiện trạng và không bảo đảm hoạt động liên tục [[10]].',
    },
  ],
  chatFallback: 'Mình không tìm thấy điều khoản nào trong hợp đồng này nói về nội dung bạn hỏi. Hợp đồng đang im lặng về vấn đề này, nên bạn có thể hỏi lại đối tác hoặc đề nghị bổ sung thành điều khoản riêng.',
}

/* ------------------------------------------------------------------ */
/* Hợp đồng mẫu 2: mua bán nội thất (Cyber/Privacy/Bên thứ ba = N/A)   */
/* ------------------------------------------------------------------ */

const furnitureFindings: Finding[] = [
  {
    id: 'F-01', severity: 'medium', domain: 'financial', secondary: ['legal'], kind: 'issue', clauseId: '4',
    title: 'Mức phạt vi phạm 12% có thể vượt mức trần luật định',
    quote: 'phải chịu phạt 12% giá trị phần nghĩa vụ bị vi phạm',
    why: 'Với hợp đồng mua bán hàng hóa giữa hai doanh nghiệp, Luật Thương mại 2005 giới hạn mức phạt vi phạm không quá 8% giá trị phần nghĩa vụ bị vi phạm.',
    impact: 'Phần vượt trần có thể không được áp dụng khi tranh chấp. Bên mình cũng có thể là bên bị phạt nếu chậm thanh toán.',
    recommendation: 'Đề nghị điều chỉnh mức phạt về tối đa 8%, kèm quyền yêu cầu bồi thường thiệt hại thực tế.',
    reference: 'Luật Thương mại 2005, Điều 301 (mức phạt vi phạm)',
  },
  {
    id: 'F-02', severity: 'medium', domain: 'operational', kind: 'issue', clauseId: '3',
    title: 'Thời hạn giao hàng có thể kéo dài không giới hạn',
    quote: 'Thời hạn giao hàng được kéo dài tương ứng nếu nguyên vật liệu về chậm',
    why: 'Bên B được kéo dài thời hạn giao hàng mỗi khi nguyên vật liệu về chậm, không có mức tối đa và không cần chứng minh.',
    impact: 'Văn phòng mới có thể chưa có bàn ghế đúng ngày chuyển vào, trong khi bên mình đã tạm ứng 30%.',
    recommendation: 'Đề nghị giới hạn thời gian gia hạn (ví dụ tối đa 10 ngày), yêu cầu thông báo bằng văn bản và quyền hủy đơn nếu trễ quá mức này.',
  },
  {
    id: 'F-03', severity: 'low', domain: 'legal', kind: 'issue', clauseId: '7',
    title: 'Tranh chấp giải quyết tại tòa án nơi đối tác đặt trụ sở',
    quote: 'Tòa án nhân dân có thẩm quyền nơi Bên B đặt trụ sở chính',
    why: 'Nếu Bên B ở tỉnh khác, bên mình phải theo kiện ở xa.',
    impact: 'Tăng chi phí đi lại khi có tranh chấp. Với giá trị hợp đồng này, ảnh hưởng thấp.',
    recommendation: 'Có thể đề nghị tòa án nơi Bên A đặt trụ sở, hoặc giữ nguyên nếu hai bên cùng tỉnh.',
  },
  {
    id: 'F-04', severity: 'low', domain: 'operational', kind: 'issue', clauseId: '5',
    title: 'Bảo hành 12 tháng',
    quote: 'Bên B bảo hành sản phẩm 12 tháng kể từ ngày nghiệm thu',
    why: 'Thời hạn bảo hành 12 tháng là mức thông thường với nội thất văn phòng.',
    impact: 'Ảnh hưởng thấp.',
    recommendation: 'Không cần thay đổi. Nếu được, có thể đề nghị 24 tháng cho khung ghế.',
  },
]

const furnitureDetail: ContractDetail = {
  heading: 'HỢP ĐỒNG MUA BÁN NỘI THẤT VĂN PHÒNG',
  number: 'Số: 0412/2026/HĐMB-VH',
  preamble: 'Hôm nay, ngày 02 tháng 10 năm 2026, tại Thành phố Hồ Chí Minh, chúng tôi gồm:',
  partyA: { label: 'Bên A (Bên mua)', name: ORG_NAME, rep: 'Ông Trần Quốc Bảo, Giám đốc' },
  partyB: { label: 'Bên B (Bên bán)', name: 'Công ty CP Nội thất Việt Hưng', rep: 'Ông Ngô Văn Thắng, Giám đốc' },
  articles: [
    { number: '1', title: 'Hàng hóa và giá trị hợp đồng', page: 1, clauses: [{ id: '1', text: 'Bên B bán cho Bên A 60 bộ bàn ghế làm việc và 12 tủ hồ sơ theo mẫu, quy cách tại Phụ lục 01. Tổng giá trị hợp đồng là 214.500.000 đồng, đã bao gồm thuế giá trị gia tăng và chi phí vận chuyển, lắp đặt.' }] },
    { number: '2', title: 'Thanh toán', page: 1, clauses: [
      { id: '2.1', text: 'Bên A tạm ứng 30% giá trị hợp đồng trong vòng 07 ngày kể từ ngày ký.' },
      { id: '2.2', text: 'Phần còn lại được thanh toán trong vòng 15 ngày kể từ ngày hai Bên ký biên bản nghiệm thu.' },
    ] },
    { number: '3', title: 'Giao hàng', page: 1, clauses: [{ id: '3', text: 'Bên B giao hàng và lắp đặt tại văn phòng của Bên A trong vòng 30 ngày kể từ ngày nhận tạm ứng. Thời hạn giao hàng được kéo dài tương ứng nếu nguyên vật liệu về chậm.' }] },
    { number: '4', title: 'Phạt vi phạm', page: 1, clauses: [{ id: '4', text: 'Bên nào vi phạm nghĩa vụ thanh toán hoặc giao hàng thì phải chịu phạt 12% giá trị phần nghĩa vụ bị vi phạm.' }] },
    { number: '5', title: 'Bảo hành', page: 2, clauses: [{ id: '5', text: 'Bên B bảo hành sản phẩm 12 tháng kể từ ngày nghiệm thu đối với lỗi do nguyên vật liệu và kỹ thuật sản xuất.' }] },
    { number: '6', title: 'Nghiệm thu', page: 2, clauses: [{ id: '6', text: 'Hai Bên tiến hành nghiệm thu số lượng và chất lượng hàng hóa ngay sau khi lắp đặt xong và ký biên bản nghiệm thu.' }] },
    { number: '7', title: 'Giải quyết tranh chấp', page: 2, clauses: [{ id: '7', text: 'Tranh chấp phát sinh được giải quyết tại Tòa án nhân dân có thẩm quyền nơi Bên B đặt trụ sở chính.' }] },
  ],
  findings: furnitureFindings,
  checks: [],
  chat: [
    {
      q: 'Mức phạt 12% có hợp lệ không?',
      keywords: ['phạt', '12%', '8%', 'hợp lệ', 'trần'],
      a: 'Có rủi ro không được áp dụng đầy đủ. Hợp đồng quy định phạt 12% giá trị phần nghĩa vụ bị vi phạm [[4]], trong khi với hợp đồng mua bán hàng hóa giữa doanh nghiệp, Luật Thương mại 2005 giới hạn mức phạt không quá 8%.\n\nĐây là rủi ro tiềm ẩn cần xem xét, không phải kết luận pháp lý. Bên mình cũng có thể bị phạt nếu chậm thanh toán, nên nên đề nghị đưa về 8%.',
    },
    {
      q: 'Nếu giao hàng trễ thì sao?',
      keywords: ['trễ', 'chậm', 'giao hàng', 'thời hạn'],
      a: 'Bên B phải giao trong 30 ngày kể từ khi nhận tạm ứng, nhưng thời hạn được kéo dài mỗi khi nguyên vật liệu về chậm [[3]], không có mức tối đa. Nếu trễ ngoài lý do đó, Bên B chịu phạt theo Điều 4 [[4]].\n\nNên đề nghị giới hạn thời gian gia hạn và quyền hủy đơn nếu trễ quá mức.',
    },
  ],
  chatFallback: 'Mình không tìm thấy điều khoản nào trong hợp đồng này nói về nội dung bạn hỏi. Bạn có thể hỏi lại đối tác hoặc đề nghị bổ sung thành điều khoản riêng.',
}

/* ------------------------------------------------------------------ */
/* Thư viện hợp đồng                                                   */
/* ------------------------------------------------------------------ */

export const CLOUD_RESULT: AnalysisResult = {
  score: 79,
  domainScores: scores([74, 72, 61, 22, 88, 86, 69]),
  counts: countOf(cloudFindings),
  clauseCount: 26,
}

const NA_REASON = 'Đối tác không tiếp nhận hay xử lý dữ liệu của bên mình (theo ngữ cảnh khai báo khi tải lên).'

export const CLOUD_DETAIL = cloudDetail

export const CONTRACTS: Contract[] = [
  {
    id: 'hd-cloud', title: 'Hợp đồng cung cấp dịch vụ điện toán đám mây', partner: 'Giải pháp Mây Nam Việt', type: 'Dịch vụ',
    value: '486.000.000 ₫/năm', pages: 5, updatedAt: '2026-10-23T16:40:00', ownerId: 'm1', status: 'review',
    context: { ourSide: 'buyer', provides: 'Phần mềm tính lương và nhân sự trên cloud', dataAccess: 'personal', criticality: 'high' },
    result: CLOUD_RESULT, detail: cloudDetail,
    versions: [
      { v: 2, date: '2026-10-23T16:40:00', by: 'Trần Quốc Bảo', score: 79, note: 'Bản đối tác gửi lại sau buổi trao đổi ngày 22/10' },
      { v: 1, date: '2026-10-16T09:05:00', by: 'Lê Thu Hà', score: 84, note: 'Bản đầu tiên đối tác gửi qua email' },
    ],
    activity: [
      { at: '2026-10-24T08:12:00', by: 'Phạm Đức Anh', text: 'đánh dấu F-13 là Cần đàm phán' },
      { at: '2026-10-23T16:42:00', by: 'Hệ thống', text: 'phân tích xong phiên bản V2: 79/100, 3 rủi ro nghiêm trọng' },
      { at: '2026-10-23T16:40:00', by: 'Trần Quốc Bảo', text: 'tải lên phiên bản V2' },
      { at: '2026-10-17T10:20:00', by: 'Trần Quốc Bảo', text: 'chia sẻ với Phạm Đức Anh' },
      { at: '2026-10-16T09:07:00', by: 'Hệ thống', text: 'phân tích xong phiên bản V1: 84/100' },
      { at: '2026-10-16T09:05:00', by: 'Lê Thu Hà', text: 'tải lên phiên bản V1' },
    ],
  },
  {
    id: 'hd-noi-that', title: 'Hợp đồng mua bán nội thất văn phòng', partner: 'Nội thất Việt Hưng', type: 'Mua bán',
    value: '214.500.000 ₫', pages: 2, updatedAt: '2026-10-21T14:10:00', ownerId: 'm2', status: 'negotiating',
    context: { ourSide: 'buyer', provides: 'Bàn ghế, tủ hồ sơ văn phòng', dataAccess: 'none', criticality: 'low' },
    result: { score: 41, domainScores: scores([28, 52, 47, 15, null, null, null]), naReason: NA_REASON, counts: countOf(furnitureFindings), clauseCount: 8 },
    detail: furnitureDetail,
    versions: [{ v: 1, date: '2026-10-03T11:30:00', by: 'Lê Thu Hà', score: 41 }],
    activity: [
      { at: '2026-10-21T14:10:00', by: 'Lê Thu Hà', text: 'chuyển trạng thái sang Đang đàm phán' },
      { at: '2026-10-03T11:31:00', by: 'Hệ thống', text: 'phân tích xong phiên bản V1: 41/100' },
      { at: '2026-10-03T11:30:00', by: 'Lê Thu Hà', text: 'tải lên phiên bản V1' },
    ],
  },
  {
    id: 'hd-sow-erp', title: 'Phụ lục 02 – SOW triển khai hệ thống ERP', partner: 'Tư vấn Số hóa Bắc Hà', type: 'SOW',
    pages: 14, updatedAt: '2026-10-24T09:58:00', ownerId: 'm3', status: 'analyzing',
    context: { ourSide: 'buyer', provides: 'Triển khai và tùy biến ERP', dataAccess: 'normal', criticality: 'high' },
    versions: [{ v: 1, date: '2026-10-24T09:58:00', by: 'Phạm Đức Anh' }],
    activity: [{ at: '2026-10-24T09:58:00', by: 'Phạm Đức Anh', text: 'tải lên phiên bản V1' }],
  },
  {
    id: 'hd-msa-mang', title: 'Hợp đồng khung cung cấp thiết bị mạng', partner: 'Thiết bị Viễn thông Hải Đăng', type: 'Hợp đồng khung',
    value: '1.250.000.000 ₫', pages: 22, updatedAt: '2026-10-19T10:00:00', ownerId: 'm3', status: 'rejected',
    context: { ourSide: 'buyer', provides: 'Thiết bị mạng và bảo trì', dataAccess: 'normal', criticality: 'medium' },
    result: { score: 68, domainScores: scores([71, 64, 66, 30, 58, 34, 49]), counts: { critical: 1, high: 6, medium: 8, low: 5 }, clauseCount: 41 },
    versions: [{ v: 1, date: '2026-10-12T15:20:00', by: 'Phạm Đức Anh', score: 68 }],
    activity: [
      { at: '2026-10-19T10:00:00', by: 'Trần Quốc Bảo', text: 'chuyển trạng thái sang Từ chối' },
      { at: '2026-10-12T15:22:00', by: 'Hệ thống', text: 'phân tích xong phiên bản V1: 68/100' },
    ],
  },
  {
    id: 'hd-quang-cao', title: 'Hợp đồng dịch vụ quảng cáo trực tuyến', partner: 'Truyền thông Mộc Lan', type: 'Dịch vụ',
    value: '96.000.000 ₫', pages: 9, updatedAt: '2026-10-22T08:30:00', ownerId: 'm5', status: 'review',
    context: { ourSide: 'buyer', provides: 'Chạy quảng cáo và báo cáo hiệu quả', dataAccess: 'normal', criticality: 'medium' },
    result: { score: 52, domainScores: scores([48, 61, 44, 20, 35, 57, 40]), counts: { critical: 0, high: 3, medium: 6, low: 4 }, clauseCount: 23 },
    versions: [{ v: 1, date: '2026-10-22T08:30:00', by: 'Đỗ Hải Yến', score: 52 }],
    activity: [{ at: '2026-10-22T08:32:00', by: 'Hệ thống', text: 'phân tích xong phiên bản V1: 52/100' }],
  },
  {
    id: 'hd-van-chuyen', title: 'Hợp đồng dịch vụ vận chuyển hàng hóa', partner: 'Vận tải Trường Sơn', type: 'Dịch vụ',
    value: '38.000.000 ₫/tháng', pages: 11, updatedAt: '2026-10-14T17:15:00', ownerId: 'm2', status: 'approved',
    context: { ourSide: 'buyer', provides: 'Vận chuyển hàng nội địa', dataAccess: 'normal', criticality: 'high' },
    result: { score: 63, domainScores: scores([66, 58, 70, 41, 25, 38, 52]), counts: { critical: 0, high: 5, medium: 7, low: 3 }, clauseCount: 29 },
    versions: [
      { v: 2, date: '2026-10-14T17:15:00', by: 'Lê Thu Hà', score: 63 },
      { v: 1, date: '2026-10-08T10:00:00', by: 'Lê Thu Hà', score: 71 },
    ],
    activity: [{ at: '2026-10-14T17:20:00', by: 'Trần Quốc Bảo', text: 'chuyển trạng thái sang Đã duyệt' }],
  },
  {
    id: 'hd-tuyen-dung', title: 'Hợp đồng dịch vụ tuyển dụng nhân sự', partner: 'Nhân lực Kim Ngân', type: 'Dịch vụ',
    value: '15% lương năm/ứng viên', pages: 7, updatedAt: '2026-10-20T13:45:00', ownerId: 'm4', status: 'negotiating',
    context: { ourSide: 'buyer', provides: 'Tìm và giới thiệu ứng viên', dataAccess: 'personal', criticality: 'medium' },
    result: { score: 46, domainScores: scores([42, 50, 38, 26, 31, 55, 36]), counts: { critical: 0, high: 2, medium: 5, low: 6 }, clauseCount: 18 },
    versions: [{ v: 1, date: '2026-10-20T13:45:00', by: 'Võ Ngọc Mai', score: 46 }],
    activity: [{ at: '2026-10-20T13:47:00', by: 'Hệ thống', text: 'phân tích xong phiên bản V1: 46/100' }],
  },
  {
    id: 'hd-bao-ve', title: 'Hợp đồng dịch vụ bảo vệ tòa nhà', partner: 'Dịch vụ Bảo vệ An Khang', type: 'Dịch vụ',
    value: '24.000.000 ₫/tháng', pages: 6, updatedAt: '2026-09-29T09:00:00', ownerId: 'm1', status: 'approved',
    context: { ourSide: 'buyer', provides: 'Bảo vệ 24/7', dataAccess: 'none', criticality: 'medium' },
    result: { score: 27, domainScores: scores([31, 24, 29, 18, null, null, null]), naReason: NA_REASON, counts: { critical: 0, high: 0, medium: 3, low: 5 }, clauseCount: 15 },
    versions: [{ v: 1, date: '2026-09-29T09:00:00', by: 'Trần Quốc Bảo', score: 27 }],
    activity: [{ at: '2026-09-29T09:30:00', by: 'Trần Quốc Bảo', text: 'chuyển trạng thái sang Đã duyệt' }],
  },
  {
    id: 'hd-van-phong-pham', title: 'Hợp đồng mua văn phòng phẩm năm 2027', partner: 'Văn phòng phẩm Phúc Lộc', type: 'Mua bán',
    value: '62.000.000 ₫', pages: 4, updatedAt: '2026-10-10T11:00:00', ownerId: 'm4', status: 'approved',
    context: { ourSide: 'buyer', provides: 'Giấy in, văn phòng phẩm', dataAccess: 'none', criticality: 'low' },
    result: { score: 18, domainScores: scores([20, 22, 15, 12, null, null, null]), naReason: NA_REASON, counts: { critical: 0, high: 0, medium: 1, low: 4 }, clauseCount: 11 },
    versions: [{ v: 1, date: '2026-10-10T11:00:00', by: 'Võ Ngọc Mai', score: 18 }],
    activity: [{ at: '2026-10-10T11:02:00', by: 'Hệ thống', text: 'phân tích xong phiên bản V1: 18/100' }],
  },
  {
    id: 'hd-thue-vp', title: 'Hợp đồng thuê văn phòng tầng 7 (bản scan)', partner: 'Đầu tư Tòa nhà Ánh Dương', type: 'Thuê',
    pages: 0, updatedAt: '2026-10-23T10:05:00', ownerId: 'm1', status: 'failed',
    error: 'Tệp PDF được bảo vệ bằng mật khẩu nên hệ thống không đọc được nội dung.',
    context: { ourSide: 'buyer', provides: 'Cho thuê mặt bằng văn phòng', dataAccess: 'none', criticality: 'high' },
    versions: [{ v: 1, date: '2026-10-23T10:05:00', by: 'Trần Quốc Bảo' }],
    activity: [{ at: '2026-10-23T10:06:00', by: 'Hệ thống', text: 'không đọc được tệp: tệp được bảo vệ bằng mật khẩu' }],
  },
]

/** Kết quả hiện ra khi hợp đồng "đang phân tích" chạy xong trong demo. */
export const SOW_RESULT: AnalysisResult = {
  score: 57,
  domainScores: scores([55, 62, 68, 24, 47, 39, 51]),
  counts: { critical: 0, high: 4, medium: 9, low: 6 },
  clauseCount: 37,
}

export const SHARES: Record<string, Share> = {
  'hd-cloud': { memberIds: ['m2', 'm3'], orgWide: false },
}

export const INITIAL_FINDING_STATES: Record<string, { state: import('../model').FindingState; note: string }> = {
  'hd-cloud:F-13': { state: 'negotiate', note: 'IT đã soạn danh sách yêu cầu bảo mật tối thiểu, gửi kèm khi đàm phán.' },
  'hd-cloud:F-23': { state: 'accepted', note: '' },
  'hd-noi-that:F-04': { state: 'accepted', note: '' },
}
