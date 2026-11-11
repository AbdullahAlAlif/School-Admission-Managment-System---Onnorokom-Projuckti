export default function SummaryCards({ items }) {
    return (
        <section className="summary-grid">
            {items.map(item => (
                <div className="summary-card" key={item.label}>
                    <div className="summary-label">{item.label}</div>
                    <div className={`summary-value ${item.valueClass || ""}`}>
                        {item.value}
                    </div>
                    <div className="summary-foot">{item.foot}</div>
                </div>
            ))}
        </section>
    );
}
