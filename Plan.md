### **Bản Kế Hoạch Nghiệp Vụ Realtime Hoàn Chỉnh**

#### **I. Nguyên tắc cốt lõi**

1.  **Phân vai rõ ràng:** Trải nghiệm realtime được cá nhân hóa cho từng vai trò (Client, Agent, Manager).
2.  **Ngữ cảnh là trên hết:** Hệ thống sẽ nhận biết ngữ cảnh của người dùng (đang ở trang nào, vị trí cuộn chuột) để đưa ra hình thức cập nhật phù hợp (animation, toast, thông báo nổi bật).
3.  **Thông tin vừa đủ, không làm phiền:** Client chỉ nhận thông báo về các thay đổi quan trọng. Agent chỉ nhận thông báo "nổi bật" khi thực sự cần chú ý.
4.  **Tương tác trực tiếp:** Các thông báo đều có tính tương tác, cho phép người dùng điều hướng nhanh đến nội dung liên quan.

---

#### **II. Chi tiết Triển khai theo Từng Tính năng**

**1. Hệ thống Thông báo Trung tâm (Frontend)**

*   **Chuông thông báo (Notification Bell):**
    *   **Hiển thị:** Một icon chuông trên header, có badge đếm số thông báo chưa đọc.
    *   **Lưu trữ:** Cache tối đa 50 thông báo gần nhất trên `localStorage` của client. Mỗi thông báo gồm: `id`, `message`, `link`, `timestamp`, `isRead`.
    *   **Tương tác:**
        *   Click vào thông báo sẽ điều hướng đến `link` và đánh dấu là `isRead: true`.
        *   Thông báo đã đọc sẽ có màu nền khác (nhạt hơn).
        *   Có nút "Đánh dấu tất cả là đã đọc".
*   **Hệ thống Toast/Snackbar:**
    *   **Loại 1 (Toast Nhỏ):** Tự động biến mất sau 3-5 giây. Dùng cho các thông tin cập nhật (vd: "Trạng thái đã đổi", "Client có cập nhật mới..."). Vẫn có thể click để điều hướng.
    *   **Loại 2 (Thông báo Nổi bật):** Không tự động biến mất. Có nút (x) để đóng. Dùng cho các sự kiện quan trọng cần sự chú ý của Agent (vd: "Bạn vừa được giao ticket mới").

**2. Cập nhật Danh sách Ticket Động (`TicketQueuePage`)**

*   **Sự kiện:** Tạo mới, Gán đi, Thay đổi trạng thái (dẫn đến chuyển tab).
*   **Logic:**
    *   **Nếu người dùng ở đầu trang (scrollTop < 100px):**
        *   **Thêm mới:** Ticket mới được thêm vào đầu danh sách với animation `slide` và `fade` từ phải sang trái.
        *   **Xóa/Gán đi:** Ticket tương ứng có animation `slide` và `fade` sang phải rồi biến mất.
        *   Các animation này sẽ chạy đồng thời nếu có nhiều sự kiện cùng lúc.
    *   **Nếu người dùng đã cuộn xuống dưới:**
        *   Hiển thị một toast nhỏ: *"Danh sách có cập nhật mới."* hoặc *"Có [X] ticket mới."*
        *   Cache dữ liệu mới vẫn được cập nhật "ngầm". Khi người dùng cuộn lên đầu hoặc tải lại, họ sẽ thấy danh sách mới nhất.

**3. Luồng Nghiệp vụ Realtime Cụ thể**

*   **Khi Client tạo ticket:**
    *   **Manager:** Nhận toast "Có ticket mới" (nếu không ở tab Unassigned) HOẶC thấy animation ticket mới bay vào (nếu đang ở tab Unassigned). Chuông thông báo +1.
*   **Khi Manager gán ticket cho Agent:**
    *   **Agent:** Nhận thông báo nổi bật/toast (tùy ngữ cảnh). Chuông thông báo +1.
    *   **Client:** Nhận toast "Ticket của bạn đã có người xử lý". Chuông thông báo +1.
    *   **Manager & các Agent khác:** Thấy thẻ ticket trên danh sách được cập nhật (tên assignee, nút Claim biến mất).
*   **Khi Agent/Client thêm bình luận:**
    *   **Người đang xem trang chi tiết:** Thấy bong bóng chat mới xuất hiện ngay lập tức.
    *   **Người không xem trang chi tiết (Client/Assignee):** Nhận toast "Có bình luận mới trong ticket #[ID]". Chuông thông báo +1.
*   **Khi Agent đổi trạng thái/ưu tiên:**
    *   **Client:** Nhận toast "Trạng thái ticket #[ID] đã đổi thành [Tên trạng thái]". Chuông thông báo +1.
    *   **Manager/Agents:** Thấy thẻ ticket trên danh sách được cập nhật (màu chip...). Nếu cần, ticket tự động di chuyển giữa các tab.

---

### **Các Bước Kỹ thuật Tiếp theo**

Giờ chúng ta đã có một bản kế hoạch nghiệp vụ vững chắc. Các bước tiếp theo sẽ là chuyển nó thành các yêu cầu kỹ thuật:

1.  **Định nghĩa Payload cho SignalR:** Thiết kế cấu trúc JSON cho mỗi sự kiện realtime (`NewTicket`, `TicketAssigned`, `NewComment`, `StatusChanged`, `TicketDeleted`).
2.  **Backend Implementation:**
    *   Triển khai logic gửi các payload này từ `TicketService` đến đúng đối tượng người dùng (qua `UserId`) hoặc nhóm (qua `GroupName`).
3.  **Frontend Implementation:**
    *   Tích hợp TanStack Query để quản lý server state.
    *   Xây dựng `NotificationProvider` để quản lý Toast và Chuông thông báo.
    *   Viết logic trong `SignalRProvider` để lắng nghe sự kiện, gọi `NotificationProvider` và cập nhật cache của TanStack Query (`queryClient.setQueryData`).
    *   Triển khai logic kiểm tra vị trí cuộn để quyết định giữa animation và toast.
```
