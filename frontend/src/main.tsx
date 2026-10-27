import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { Link, Navigate, Outlet, RouterProvider, createBrowserRouter } from 'react-router'
import { AppShell, EmptyState } from './components/AppShell'
import { buttonClass } from './components/ui'
import { StoreProvider } from './demo/store'
import './index.css'
import Library from './pages/Library'
import Login from './pages/Login'
import Members from './pages/Members'
import NewContract from './pages/NewContract'
import Report from './pages/Report'
import Workspace from './pages/workspace/Workspace'

function NotFound() {
  return (
    <AppShell>
      <EmptyState
        title="Không tìm thấy trang"
        action={
          <Link to="/hop-dong" className={buttonClass('secondary')}>
            Về thư viện hợp đồng
          </Link>
        }
      >
        Đường dẫn có thể đã thay đổi.
      </EmptyState>
    </AppShell>
  )
}

const router = createBrowserRouter([
  {
    element: (
      <StoreProvider>
        <Outlet />
      </StoreProvider>
    ),
    children: [
      { path: '/', element: <Navigate to="/hop-dong" replace /> },
      { path: '/dang-nhap', element: <Login /> },
      { path: '/hop-dong', element: <Library /> },
      { path: '/hop-dong/moi', element: <NewContract /> },
      { path: '/hop-dong/:id', element: <Workspace /> },
      { path: '/hop-dong/:id/bao-cao', element: <Report /> },
      { path: '/to-chuc', element: <Members /> },
      { path: '*', element: <NotFound /> },
    ],
  },
])

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <RouterProvider router={router} />
  </StrictMode>,
)
