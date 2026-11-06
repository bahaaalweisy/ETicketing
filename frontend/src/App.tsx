import { useCallback, useEffect, useState } from "react";
import {
  ApiError,
  checkout,
  createBooking,
  getTickets,
  getTransaction,
  type PaymentMethod,
  type Ticket,
  type Transaction,
} from "./api";

const POLL_INTERVAL_MS = 2000;
const MAX_QUANTITY = 10;

type Phase = "selecting" | "submitting" | "pending" | "success";

const wait = (milliseconds: number) => new Promise((resolve) => setTimeout(resolve, milliseconds));

function describeError(error: unknown): string {
  if (error instanceof ApiError) {
    return error.status === 409 ? `Sorry, this could not be booked. ${error.message}` : error.message;
  }

  return "The server could not be reached. Please try again.";
}

export default function App() {
  const [tickets, setTickets] = useState<Ticket[]>([]);
  const [ticketId, setTicketId] = useState<number | null>(null);
  const [quantity, setQuantity] = useState(1);
  const [method, setMethod] = useState<PaymentMethod>("CreditCard");
  const [phase, setPhase] = useState<Phase>("selecting");
  const [error, setError] = useState<string | null>(null);
  const [transaction, setTransaction] = useState<Transaction | null>(null);

  const loadTickets = useCallback(async () => {
    try {
      setTickets(await getTickets());
    } catch (loadError) {
      setError(describeError(loadError));
    }
  }, []);

  useEffect(() => {
    void loadTickets();
  }, [loadTickets]);

  const chosen = tickets.find((ticket) => ticket.id === ticketId);
  const selected = chosen && chosen.available > 0 ? chosen : undefined;
  const maxQuantity = Math.min(MAX_QUANTITY, selected?.available ?? MAX_QUANTITY);
  const orderQuantity = Math.min(quantity, maxQuantity);

  function selectTicket(ticket: Ticket) {
    setTicketId(ticket.id);
    setQuantity(1);
    setError(null);
  }

  async function waitForResult(transactionId: string): Promise<Transaction> {
    for (;;) {
      const current = await getTransaction(transactionId);
      if (current.status !== "Pending") {
        return current;
      }

      setPhase("pending");
      await wait(POLL_INTERVAL_MS);
    }
  }

  async function handleCheckout() {
    if (!selected) {
      return;
    }

    setPhase("submitting");
    setError(null);

    try {
      const order = await createBooking(selected.id, orderQuantity, crypto.randomUUID());
      const payment = await checkout(order.id, method);
      const result = await waitForResult(payment.transactionId);

      if (result.status === "Failed") {
        setError("The payment failed. Your tickets were not charged.");
        setPhase("selecting");
      } else {
        setTransaction(result);
        setPhase("success");
      }
    } catch (checkoutError) {
      setError(describeError(checkoutError));
      setPhase("selecting");
    }

    await loadTickets();
  }

  function startOver() {
    setTransaction(null);
    setTicketId(null);
    setQuantity(1);
    setPhase("selecting");
  }

  if (phase === "success" && transaction) {
    return (
      <main className="page">
        <section className="panel success">
          <h1>Payment successful</h1>
          <dl>
            <dt>Transaction ID</dt>
            <dd className="mono">{transaction.transactionId}</dd>
            <dt>Timestamp</dt>
            <dd>{new Date(transaction.completedAt!).toLocaleString()}</dd>
            <dt>Amount</dt>
            <dd>
              {transaction.amount} {chosen?.currency}
            </dd>
          </dl>
          <button className="primary" onClick={startOver}>
            Buy more tickets
          </button>
        </section>
      </main>
    );
  }

  if (phase === "pending") {
    return (
      <main className="page">
        <section className="panel pending" role="status">
          <div className="spinner" aria-hidden="true" />
          <h1>Waiting for QR payment</h1>
          <p>This usually takes about 8 seconds. Please keep this page open.</p>
        </section>
      </main>
    );
  }

  const isBusy = phase === "submitting";

  return (
    <main className="page">
      <header>
        <h1>E-Ticketing</h1>
        <p>Choose a ticket, then pay by card or QR.</p>
      </header>

      {error && (
        <p className="error" role="alert">
          {error}
        </p>
      )}

      <section className="tickets" aria-label="Tickets">
        {tickets.map((ticket) => {
          const soldOut = ticket.available === 0;

          return (
            <button
              key={ticket.id}
              className={`ticket ${ticket.id === selected?.id ? "selected" : ""}`}
              aria-pressed={ticket.id === selected?.id}
              disabled={soldOut || isBusy}
              onClick={() => selectTicket(ticket)}
            >
              <span className="ticket-name">{ticket.name}</span>
              <span className="ticket-price">
                {ticket.price} {ticket.currency}
              </span>
              <span className="ticket-stock">{soldOut ? "Sold out" : `${ticket.available} available`}</span>
            </button>
          );
        })}
      </section>

      {selected && (
        <section className="panel">
          <label className="field">
            Quantity
            <select value={orderQuantity} disabled={isBusy} onChange={(event) => setQuantity(Number(event.target.value))}>
              {Array.from({ length: maxQuantity }, (_, index) => index + 1).map((value) => (
                <option key={value} value={value}>
                  {value}
                </option>
              ))}
            </select>
          </label>

          <fieldset className="field" disabled={isBusy}>
            <legend>Payment method</legend>
            <label>
              <input
                type="radio"
                name="method"
                checked={method === "CreditCard"}
                onChange={() => setMethod("CreditCard")}
              />
              Credit Card
            </label>
            <label>
              <input type="radio" name="method" checked={method === "QR"} onChange={() => setMethod("QR")} />
              QR
            </label>
          </fieldset>

          <p className="total">
            Total: {selected.price * orderQuantity} {selected.currency}
          </p>

          <button className="primary" disabled={isBusy} onClick={() => void handleCheckout()}>
            {isBusy ? "Processing..." : "Checkout"}
          </button>
        </section>
      )}
    </main>
  );
}
