export type PaymentMethod = "CreditCard" | "QR";
export type TransactionStatus = "Pending" | "Completed" | "Failed";

export interface Ticket {
  id: number;
  code: string;
  name: string;
  price: number;
  currency: string;
  available: number;
}

export interface Order {
  id: string;
  totalAmount: number;
}

export interface Checkout {
  orderId: string;
  transactionId: string;
  status: TransactionStatus;
}

export interface Transaction {
  transactionId: string;
  status: TransactionStatus;
  amount: number;
  completedAt: string | null;
}

interface Problem {
  title?: string;
  detail?: string;
  errors?: Record<string, string[]>;
}

/** A non-success API response, carrying the HTTP status and a readable message. */
export class ApiError extends Error {
  constructor(
    public readonly status: number,
    message: string,
  ) {
    super(message);
  }
}

const apiUrl = import.meta.env.VITE_API_URL;

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`${apiUrl}${path}`, init);

  if (!response.ok) {
    throw new ApiError(response.status, await readProblem(response));
  }

  return response.json() as Promise<T>;
}

async function readProblem(response: Response): Promise<string> {
  const problem = (await response.json().catch(() => ({}))) as Problem;
  const validationErrors = Object.values(problem.errors ?? {}).flat();

  return validationErrors.join(" ") || problem.detail || problem.title || "The request failed.";
}

export const getTickets = () => request<Ticket[]>("/api/tickets");

export const createBooking = (ticketId: number, quantity: number, idempotencyKey: string) =>
  request<Order>("/api/bookings", {
    method: "POST",
    headers: { "Content-Type": "application/json", "Idempotency-Key": idempotencyKey },
    body: JSON.stringify({ ticketId, quantity }),
  });

export const checkout = (orderId: string, paymentMethod: PaymentMethod) =>
  request<Checkout>("/api/checkout", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ orderId, paymentMethod }),
  });

export const getTransaction = (id: string) => request<Transaction>(`/api/transactions/${id}`);
