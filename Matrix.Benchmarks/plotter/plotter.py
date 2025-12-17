import pandas as pd
import matplotlib.pyplot as plt

df = pd.read_csv('../BenchmarkDotNet.Artifacts/results/Matrix.Benchmarks.MatrixBenchmarks-report.csv', 
                 delimiter=';')

def convert_to_us(time_str):
    """Конвертирует время из ns/us/ms в микросекунды"""
    time_str = str(time_str).strip()
    if 'ns' in time_str:
        return float(time_str.replace('ns', '').replace(',', '')) / 1000
    elif 'us' in time_str or 'μs' in time_str:
        return float(time_str.replace('us', '').replace('μs', '').replace(',', ''))
    elif 'ms' in time_str:
        return float(time_str.replace('ms', '').replace(',', '')) * 1000
    return float(time_str.replace(',', ''))

df['Mean_us'] = df['Mean'].apply(convert_to_us)


# Graph 1
fill_methods = df[df['Method'].str.contains('Fill', na=False)]

plt.figure(figsize=(12, 6))
for method in fill_methods['Method'].unique():
    data = fill_methods[fill_methods['Method'] == method]
    plt.plot(data['Size'], data['Mean_us'], marker='o', label=method, linewidth=2)

plt.xlabel('Размер матрицы (N×N)', fontsize=12)
plt.ylabel('Время (микросекунды)', fontsize=12)
plt.title('Сравнение производительности Fill операций', fontsize=14, fontweight='bold')
plt.legend()
plt.grid(True, alpha=0.3)
plt.yscale('log')
plt.tight_layout()
plt.savefig('fill_comparison.png', dpi=300)
plt.show()


# Graph 2
foreach_methods = df[df['Method'].str.contains('ForEach', na=False)]

plt.figure(figsize=(12, 6))
for method in foreach_methods['Method'].unique():
    data = foreach_methods[foreach_methods['Method'] == method]
    plt.plot(data['Size'], data['Mean_us'], marker='s', label=method, linewidth=2)

plt.xlabel('Размер матрицы (N×N)', fontsize=12)
plt.ylabel('Время (микросекунды)', fontsize=12)
plt.title('Сравнение производительности ForEach операций', fontsize=14, fontweight='bold')
plt.legend()
plt.grid(True, alpha=0.3)
plt.yscale('log')
plt.tight_layout()
plt.savefig('foreach_comparison.png', dpi=300)
plt.show()

# Graph 3
read_methods = df[df['Method'].isin(['GetRow', 'GetColumn'])]
plt.figure(figsize=(12, 6))
for method in read_methods['Method'].unique():
    data = read_methods[read_methods['Method'] == method]
    plt.plot(data['Size'], data['Mean_us'], marker='^', label=method, linewidth=2)

plt.xlabel('Размер матрицы (N×N)', fontsize=12)
plt.ylabel('Время (микросекунды)', fontsize=12)
plt.title('Производительность операций чтения', fontsize=14, fontweight='bold')
plt.legend()
plt.grid(True, alpha=0.3)
plt.yscale('log')
plt.tight_layout()
plt.savefig('read_operations.png', dpi=300)
plt.show()

# Graph 4
allocated_data = df[df['Allocated'].notna()]

fill_methods = df[df['Method'].str.contains('Fill', na=False)]
plt.figure(figsize=(12, 6))
for method in fill_methods['Method'].unique():
    data = allocated_data[allocated_data['Method'] == method]
    if not data.empty:
        allocated_kb = data['Allocated'].str.replace(' B', '').str.replace(',', '').astype(float) / 1024
        plt.plot(data['Size'], allocated_kb, marker='o', label=method, linewidth=2)

plt.xlabel('Размер матрицы (N×N)', fontsize=12)
plt.ylabel('Выделенная память (КБ)', fontsize=12)
plt.title('Использование памяти', fontsize=14, fontweight='bold')
plt.legend()
plt.grid(True, alpha=0.3)
plt.tight_layout()
plt.savefig('memory_allocation.png', dpi=300)
plt.show()
