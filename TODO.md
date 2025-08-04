### **I. Các Nội Dung Cần Bổ Sung Vào Báo Cáo**

File PDF của bạn đã có một nền tảng tốt, tuy nhiên, mã nguồn hiện tại đã phát triển vượt xa những gì được mô tả. Bổ sung các mục sau sẽ giúp báo cáo phản ánh đúng thực tế và thể hiện được toàn bộ công sức của nhóm.

---

#### **1. Cập nhật và Chi tiết hóa Hệ thống Phân quyền (Quan trọng nhất)**

Báo cáo hiện tại mô tả các vai trò chung chung (Client, Technician, Admin). Tuy nhiên, hệ thống của bạn đã triển khai một cơ chế phân quyền dựa trên quyền (Permission-Based) rất chi tiết và linh hoạt, đây là một điểm cộng lớn về mặt kỹ thuật.

**Nội dung cần bổ sung:**

*   **Giới thiệu Hệ thống Phân quyền dựa trên Policy và Permission:** Giải thích rằng thay vì chỉ dựa vào vai trò, hệ thống sử dụng các "permission" (quyền) cụ thể (ví dụ: `tickets:create`, `groups:manage`). Các quyền này được gán cho người dùng thông qua vai trò của họ trong file `AuthController.cs`. Điều này giúp hệ thống linh hoạt hơn rất nhiều.
*   **Cập nhật các Vai trò Người dùng:** Các vai trò đã được định nghĩa lại một cách rõ ràng hơn:
    *   **Technician** -> **Agent**: Đây là thay đổi tên gọi cơ bản.
    *   **Manager** được tách thành nhiều vai trò chuyên biệt hơn:
        *   **Group Manager**: Quản lý thành viên trong nhóm của mình, gán ticket cho thành viên.
        *   **Ticket Manager**: Quản lý tất cả các ticket, có quyền phân loại và gán ticket vào các nhóm hỗ trợ.
        *   **Manager** (User Manager): Quản lý người dùng, vai trò và các yêu cầu cấp quyền.
        *   **Admin**: Có toàn bộ quyền hạn.
*   **Tạo Bảng Phân quyền Chi tiết:** Bạn nên tạo một bảng để mô tả rõ ràng vai trò nào có những quyền (permission) nào. Ví dụ:

| Vai trò          | Các Quyền (Permissions) Chính                                                                                               |
| ----------------- | --------------------------------------------------------------------------------------------------------------------------- |
| Client            | `tickets:create`, `tickets:read_own`, `tickets:add_comment`                                                                 |
| Agent             | Bao gồm quyền của Client + `tickets:read_queue`, `tickets:update_status`, `tickets:claim`, `permissions:request`            |
| Group Manager     | Bao gồm quyền của Agent + `tickets:assign_to_member`, `tickets:reject_from_group`                                           |
| Ticket Manager    | Bao gồm quyền của Agent + `tickets:read_all`, `tickets:assign_to_group`, `problemtypes:manage`                               |
| Manager           | `users:manage`, `users:read`, `groups:manage`, `permissions:review`                                                         |
| Admin             | Tất cả các quyền trên + `users:delete`, `tickets:delete`                                                                    |

---

#### **2. Chức năng Quản lý Loại sự cố (Problem Types)**

Đây là một chức năng hoàn toàn mới và rất hữu ích chưa được đề cập trong phần "Phân tích và Thiết kế Hệ thống" của báo cáo.

**Nội dung cần bổ sung:**

*   **Mô tả chức năng:** Thêm một mục mô tả chức năng "Quản lý Loại sự cố" dành cho vai trò `Ticket Manager` và `Admin`.
*   **Mục đích:** Giải thích rằng chức năng này cho phép người quản trị có thể Thêm/Sửa/Xóa các loại sự cố (ví dụ: Lỗi phần cứng, Lỗi phần mềm, Yêu cầu mạng).
*   **Lợi ích:** Nhấn mạnh lợi ích của việc này là giúp **tự động định tuyến ticket**. Khi tạo một `ProblemType`, người quản trị có thể gán nó vào một `Group` hỗ trợ mặc định. Khi người dùng tạo ticket và chọn loại sự cố đó, ticket sẽ tự động được đưa vào hàng đợi của nhóm hỗ trợ tương ứng, giúp giảm thời gian phân loại thủ công.

---

#### **3. Chi tiết hóa các Luồng xử lý Ticket Nâng cao**

Báo cáo đã liệt kê các hành động cơ bản, nhưng mã nguồn của bạn có những luồng xử lý tinh vi hơn.

**Nội dung cần bổ sung:**

*   **Nhận Ticket (Claim Ticket):** Mô tả luồng một `Agent` trong nhóm thấy một ticket chưa được gán trong hàng đợi của nhóm và có thể bấm "Nhận" (Claim) để tự gán ticket đó cho bản thân.
*   **Đẩy Ticket khỏi Nhóm (Reject from Group):** Mô tả luồng một `Group Manager` có thể đẩy một ticket ra khỏi nhóm của mình nếu nó bị gán sai. Ticket này sẽ quay trở lại hàng đợi chung để `Ticket Manager` phân loại lại.
*   **Phân biệt Gán vào Nhóm và Gán cho Thành viên:**
    *   **Gán vào Nhóm (Assign to Group):** Chức năng của `Ticket Manager`, dùng để phân loại ticket từ hàng đợi chung vào một nhóm hỗ trợ cụ thể.
    *   **Gán cho Thành viên (Assign to Member):** Chức năng của `Group Manager`, dùng để gán ticket (đã thuộc nhóm mình) cho một `Agent` cụ thể trong nhóm.

---

#### **4. Cải tiến Giao diện Hàng đợi Ticket (Ticket Queue)**

Giao diện `TicketQueuePage` trong code phức tạp và hữu ích hơn nhiều so với mô tả "Xem hàng đợi ticket" đơn thuần.

**Nội dung cần bổ sung:**

*   **Giao diện theo Tab:** Mô tả giao diện được chia thành các tab để giúp người dùng dễ dàng lọc và quản lý ticket, ví dụ:
    *   **Assigned to Me:** Các ticket được gán cho cá nhân `Agent`.
    *   **Active in My Groups:** Các ticket đang hoạt động trong nhóm của `Agent`.
    *   **Unassigned:** (Dành cho `Ticket Manager`) Các ticket mới tạo, chưa được phân vào nhóm nào.
    *   **All Tickets:** (Dành cho `Ticket Manager`/`Admin`) Xem toàn bộ ticket trong hệ thống.
*   **Bộ lọc Nâng cao:** Đề cập đến khả năng lọc ticket theo từ khóa, mức độ ưu tiên (Priority), và trạng thái (Status).

---

#### **5. Hệ thống Thông báo Thời gian thực (Notifications)**

Báo cáo có nhắc đến SignalR, nhưng bạn nên mô tả cụ thể nó được thể hiện trên giao diện người dùng như thế nào.

**Nội dung cần bổ sung:**

*   **Mô tả thành phần Giao diện:** Có một biểu tượng chuông thông báo (`NotificationBell`) trên thanh header, hiển thị số lượng thông báo chưa đọc. Khi nhấp vào, một danh sách các thông báo gần đây sẽ hiện ra.
*   **Các Sự kiện Kích hoạt Thông báo:** Liệt kê các sự kiện quan trọng sẽ gửi thông báo đến người dùng liên quan:
    *   Khi có ticket mới được tạo.
    *   Khi có bình luận mới trong ticket họ tham gia.
    *   Khi ticket được gán cho họ hoặc nhóm của họ.
    *   Khi trạng thái ticket của họ thay đổi.
    *   Khi một ticket mới được thêm vào hàng đợi (kích hoạt hiệu ứng animation trên card).